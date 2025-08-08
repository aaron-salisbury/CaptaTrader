using CaptaTrader.Business.Modules.Trading.DTOs;
using CaptaTrader.Business.Modules.Trading.ExternalServices;
using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using CaptaTrader.Data.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static CaptaTrader.Business.Modules.Trading.DTOs.TradeOrder;

namespace CaptaTrader.Business.Modules.Trading.DomainServices;

internal sealed class BitcoinBotTrader : IBotTrader
{
    private const string BITCOIN_SYMBOL = "BTC";
    private const decimal TRADING_FEE_SPOT = 0.001M;
    private const decimal TRADING_FEE_INSTANT = 0.005M;

    //TODO: Save price history to DB and only call API to update once stale.
    internal PriceInterval? PriceIntervalMonth { get; set; }
    internal PriceInterval? PriceIntervalDay { get; set; }

    private readonly IMarketDataService _cryptoData;
    private readonly IExchangeService _exchange;
    private readonly ITradeAnalyzer _tradeAnalyzer;
    private readonly ISQLDataAccess _dataAccess;
    private readonly IUserAccessService _userAccess;
    private readonly IValidator<TradeOrder> _tradeValidator;
    private readonly ILogger _logger;

    public BitcoinBotTrader(IMarketDataService cryptoDataService, IExchangeService exchangeService, ITradeAnalyzer tradeAnalyzerService, ISQLDataAccess dataAccess,
        IUserAccessService userAccessService, IValidator<TradeOrder> tradeValidator, ILogger logger)
    {
        _cryptoData = cryptoDataService;
        _exchange = exchangeService;
        _tradeAnalyzer = tradeAnalyzerService;
        _dataAccess = dataAccess;
        _userAccess = userAccessService;
        _tradeValidator = tradeValidator;
        _logger = logger;
    }

    public async Task<IEnumerable<TradeOrder>> AttemptToInitiateTradesAsync()
    {
        ConcurrentBag<TradeOrder> tradeOrders = [];

        // 1. Verify user.
        long? userId = _userAccess.GetSessionEndUserId();
        if (userId == null)
        {
            _logger.LogError("User is not authenticated. Cannot proceed with trade.");
            return [];
        }

        // 2. Get asset balances.
        (AssetBalance? currency, AssetBalance? btc) = await GetAssetBalancesAsync();
        if (currency == null || btc == null)
        {
            _logger.LogError("Failed to update balances. Cannot proceed with trade.");
            return [];
        }

        // 3. Get current Bitcoin price.
        decimal? bitcoinPrice = await _exchange.GetSpotPriceAsync(BITCOIN_SYMBOL);
        if (bitcoinPrice == null)
        {
            _logger.LogError("Failed to retrieve Bitcoin price. Cannot proceed with trade.");
            return [];
        }

        // 4. In parallel, conduct analysis for potential buying/selling of Bitcoin. Execute trade when possible.
        async Task AnalyzeAndExecute(Func<Task<TradeOrder?>> analyzeFunc)
        {
            try
            {
                TradeOrder? order = await analyzeFunc();
                if (order != null)
                {
                    if (await AttemptTradeExecutionAsync(order))
                    {
                        tradeOrders.Add(order);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during trade analysis or execution.");
            }
        }

        Task buyTask = AnalyzeAndExecute(() => _tradeAnalyzer.AnalyzePotentialBuyAsync(userId.Value, BITCOIN_SYMBOL, currency, bitcoinPrice.Value));
        Task sellTask = AnalyzeAndExecute(() => _tradeAnalyzer.AnalyzePotentialSellAsync(userId.Value, btc, currency.Asset, bitcoinPrice.Value));

        await Task.WhenAll(buyTask, sellTask);

        return tradeOrders;
    }

    private async Task<bool> AttemptTradeExecutionAsync(TradeOrder order)
    {
        ValidationResult validationResult = await _tradeValidator.ValidateAsync(order);
        if (!validationResult.IsValid)
        {
            _logger.LogError("Trade order validation failed: {errors}", validationResult.ToString());
            return false;
        }

        order.TimeStamp = order.TransactionType == TransactionTypes.Buy 
            ? await _exchange.BuyAsync(order) 
            : await _exchange.SellAsync(order);

        if (order.TimeStamp != null)
        {
            _ = SaveOrderAsync(order);
            return true;
        }

        return false;
    }

    private async Task<(AssetBalance? Currency, AssetBalance? Btc)> GetAssetBalancesAsync()
    {
        AssetBalance? currency = null;
        AssetBalance? btc = null;
        string exchangeMediumSymbol = _exchange.GetExchangeMediumSymbol();
        IEnumerable<AssetBalance>? assetBalances = await _exchange.GetAccountBalancesAsync([exchangeMediumSymbol, BITCOIN_SYMBOL]);

        if (assetBalances != null)
        {
            foreach (AssetBalance assetBalance in assetBalances)
            {
                if (string.Equals(assetBalance.Asset, exchangeMediumSymbol))
                {
                    currency = assetBalance;
                }
                else if (string.Equals(assetBalance.Asset, BITCOIN_SYMBOL))
                {
                    btc = assetBalance;
                }
            }
        }

        return (currency, btc);
    }

    private async Task<bool> SaveOrderAsync(TradeOrder order)
    {
        try
        {
            long? orderId = await _dataAccess.CreateAsync(order);
            return orderId != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist trade order asynchronously. {order}", order);
            return false;
        }
    }



    private async Task<bool> UpdatePriceIntervalsAsync()
    {
        bool successful = false;

        IEnumerable<PriceInterval>? priceIntervals = await _cryptoData.GetLastThirtyDaysOfPriceIntervals(_exchange.GetExchangeID(), BITCOIN_SYMBOL);

        DateTime dateTimeTodayStart = DateTime.Now.Date;
        if (priceIntervals != null && priceIntervals.Any())
        {
            PriceIntervalDay = priceIntervals.Where(pi => pi.StartDate.Date == dateTimeTodayStart).FirstOrDefault();

            if (PriceIntervalDay == null)
            {
                dateTimeTodayStart.AddDays(-1);
                PriceIntervalDay = priceIntervals.Where(pi => pi.StartDate.Date == dateTimeTodayStart).FirstOrDefault();
            }

            if (PriceIntervalDay != null)
            {
                PriceInterval oldest = priceIntervals.Last();
                PriceIntervalMonth = new PriceInterval()
                {
                    Symbol = BITCOIN_SYMBOL,
                    StartDate = oldest.StartDate,
                    EndDate = PriceIntervalDay.EndDate,
                    StartPrice = Convert.ToDecimal(oldest.StartPrice),
                    EndPrice = Convert.ToDecimal(PriceIntervalDay.EndPrice)
                };
            }

            successful = true;
        }

        return successful;
    }
}
