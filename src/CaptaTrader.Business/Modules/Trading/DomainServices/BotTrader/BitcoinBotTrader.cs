using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Modules.Trading.DTOs;
using CaptaTrader.Business.Modules.Trading.ExternalServices;
using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using CaptaTrader.Data.Services;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.DomainServices;

public sealed class BitcoinBotTrader : IBotTrader
{
    private const string BITCOIN_SYMBOL = "BTC";
    private const string QUOTE_ASSET = "USDT";
    private const decimal TRADING_FEE_SPOT = 0.001M;
    private const decimal TRADING_FEE_INSTANT = 0.005M;

    //TODO: Save price history to DB and only call API to update once stale.
    internal PriceInterval? PriceIntervalMonth { get; set; }
    internal PriceInterval? PriceIntervalDay { get; set; }

    private readonly IMarketDataService _cryptoDataService;
    private readonly IExchangeService _exchangeService;
    private readonly ITradeAnalyzerService _tradeAnalyzerService;
    private readonly IDataAccess _dataAccess;
    private readonly IUserAccessService _userAccessService;
    private IValidator<TradeOrderDto> _tradeValidator;
    private ILogger _logger;

    public BitcoinBotTrader(IMarketDataService cryptoDataService, IExchangeService exchangeService, ITradeAnalyzerService tradeAnalyzerService, IDataAccess dataAccess,
        IUserAccessService userAccessService, IValidator<DTOs.TradeOrderDto> tradeValidator, ILogger logger)
    {
        _cryptoDataService = cryptoDataService;
        _exchangeService = exchangeService;
        _tradeAnalyzerService = tradeAnalyzerService;
        _dataAccess = dataAccess;
        _userAccessService = userAccessService;
        _tradeValidator = tradeValidator;
        _logger = logger;
    }

    public async Task<bool> UpdateBalancesAsync()
    {
        bool successful = false;
        IEnumerable<AssetBalance>? assetBalances = await _exchangeService.GetAccountBalancesAsync(new List<string>() { QUOTE_ASSET, BITCOIN_SYMBOL });

        if (assetBalances != null)
        {
            foreach (AssetBalance balances in assetBalances)
            {
                if (string.Equals(balances.Asset, QUOTE_ASSET))
                {
                    //Portfolio.AccountBalance = balances.Available;
                }
                else if (string.Equals(balances.Asset, BITCOIN_SYMBOL))
                {
                    //Portfolio.AssetBalance = balances.Available;
                }
            }

            successful = true;
        }

        return successful;
    }

    public async Task<bool> UpdatePriceIntervalsAsync()
    {
        bool successful = false;

        IEnumerable<PriceInterval>? priceIntervals = await _cryptoDataService.GetLastThirtyDaysOfPriceIntervals(_exchangeService.GetExchangeID(), BITCOIN_SYMBOL);

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

    public async Task<DTOs.TradeOrderDto?> AttemptToInitiateTradeAsync()
    {
        //TODO: Temporary for testing.
        //  Will need to answer whether I should sell, buy, or hold?

        DTOs.TradeOrderDto? tradeOrder = _tradeAnalyzerService.AnalyzePotentialTrade();
        int? userId = _userAccessService.GetSessionEndUserId();

        if (tradeOrder != null
            && userId != null
            && (tradeOrder.TransactionType == DTOs.TradeOrderDto.TransactionTypes.Buy && _exchangeService.Buy(tradeOrder)
                || tradeOrder.TransactionType == DTOs.TradeOrderDto.TransactionTypes.Sell && _exchangeService.Sell(tradeOrder)))
        {
            ValidationResult result = await _tradeValidator.ValidateAsync(tradeOrder);
            if (!result.IsValid)
            {
                _logger.LogError(result.ToStringPretty("Trade Order creation"));
                return null;
            }

            _dataAccess.Create(new Data.Entities.TradeOrder()
            {
                EndUserId = userId.Value,
                TimeStamp = DateTime.Now,
                TransactionType = Convert.ToInt32(tradeOrder.TransactionType),
                Symbol = tradeOrder.Symbol,
                PriceUSDT = tradeOrder.PriceUSDT,
                Amount = tradeOrder.Amount
            });

            //await UpdateBalancesAsync();
        }

        return tradeOrder;
    }
}
