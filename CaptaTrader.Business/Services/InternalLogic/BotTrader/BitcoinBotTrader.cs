using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Data.Base;
using CaptaTrader.Data.Database;
using CaptaTrader.Models;
using Microsoft.Extensions.Logging;

namespace CaptaTrader.Business.Services;

public class BitcoinBotTrader : IBotTrader
{
    private const string BITCOIN_SYMBOL = "BTC";
    private const string QUOTE_ASSET = "USDT";
    private const decimal TRADING_FEE_SPOT = 0.001M;
    private const decimal TRADING_FEE_INSTANT = 0.005M;

    //TODO: Save price history to DB and only call API to update once stale.
    internal PriceInterval? PriceIntervalMonth { get; set; }
    internal PriceInterval? PriceIntervalDay { get; set; }

    private readonly ILogger _logger;
    private readonly IMarketDataService _cryptoDataService;
    private readonly IExchangeService _exchangeService;
    private readonly ITradeAnalyzerService _tradeAnalyzerService;
    private readonly ApplicationDbContext _context;
    private readonly ISessionValueProvider _sessionValueProvider;

    public BitcoinBotTrader(IMarketDataService cryptoDataService, IExchangeService exchangeService, ITradeAnalyzerService tradeAnalyzerService, ILogger logger, ApplicationDbContext context, ISessionValueProvider sessionValueProvider)
    {
        _cryptoDataService = cryptoDataService;
        _exchangeService = exchangeService;
        _tradeAnalyzerService = tradeAnalyzerService;
        _logger = logger;
        _context = context;
        _sessionValueProvider = sessionValueProvider;
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

    public async Task<TradeOrder?> AttemptToInitiateTradeAsync()
    {
        //TODO: Temporary for testing.
        //  Will need to answer whether I should sell, buy, or hold?

        TradeOrder? tradeOrder = _tradeAnalyzerService.AnalyzePotentialTrade();
        int? userId = _sessionValueProvider.GetUserID();

        if (tradeOrder != null 
            && userId != null
            && ((tradeOrder.TransactionType == TradeOrder.TransactionTypes.Buy && _exchangeService.Buy(tradeOrder))
                || (tradeOrder.TransactionType == TradeOrder.TransactionTypes.Sell && _exchangeService.Sell(tradeOrder))))
        {
            _context.ExecutedTrades.Add(new ExecutedTrade()
            {
                EndUserId = userId.Value,
                TradeOrder = tradeOrder
            });
            _context.Save();

            //await UpdateBalancesAsync();
        }

        return tradeOrder;
    }

    public async Task WriteTradeOrdersToCSVAsync()
    {
        List<TradeOrder> trades = [.. _context.ExecutedTrades
            .Where(et => et.EndUserId == _sessionValueProvider.GetUserID())
            .Select(et => et.TradeOrder)
            .OrderByDescending(to => to.TimeStamp)];

        await IO.WriteToCSVAsync(trades, _logger);
    }
}
