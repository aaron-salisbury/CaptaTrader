using CaptaTrader.Business.Jobs;
using CaptaTrader.Business.Models;
using CaptaTrader.Data;
using CaptaTrader.Data.Base;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CaptaTrader.Business.Services
{
    public class BitcoinBotTraderService : BaseBotTraderService
    {
        private const string BITCOIN_SYMBOL = "BTC";
        private const string QUOTE_ASSET = "USDT";
        private const decimal TRADING_FEE_SPOT = 0.001M;
        private const decimal TRADING_FEE_INSTANT = 0.005M;

        private readonly ILogger _logger;
        private readonly IMarketDataService _cryptoDataService;
        private readonly IExchangeService _exchangeService;
        private readonly ITradeAnalyzerService _tradeAnalyzerService;
        private readonly AppDataAccessor _dataAccessor;

        public BitcoinBotTraderService(IMarketDataService cryptoDataService, IExchangeService exchangeService, ITradeAnalyzerService tradeAnalyzerService, ILogger logger, AppDataAccessor dataAccessor) : base()
        {
            _cryptoDataService = cryptoDataService;
            _exchangeService = exchangeService;
            _tradeAnalyzerService = tradeAnalyzerService;
            _logger = logger;
            _dataAccessor = dataAccessor;
        }

        public override async Task<bool> InitializeAsync()
        {
            bool initialized = false;

            if (UpdateBalancesAsync().Result && UpdatePriceIntervalsAsync().Result)
            {
                await ScheduleJobsAsync();

                initialized = true;
            }

            return initialized;
        }

        public override async Task ShutdownAsync()
        {
            await UnscheduleJobsAsync();
        }

        public override async Task<bool> UpdateBalancesAsync()
        {
            bool successful = false;
            IEnumerable<AssetBalance>? assetBalances = await _exchangeService.GetAccountBalancesAsync(new List<string>() { QUOTE_ASSET, BITCOIN_SYMBOL });

            if (assetBalances != null)
            {
                foreach (AssetBalance balances in assetBalances)
                {
                    if (string.Equals(balances.Asset, QUOTE_ASSET))
                    {
                        RunningPortfolio.AccountBalance = balances.Available;
                    }
                    else if (string.Equals(balances.Asset, BITCOIN_SYMBOL))
                    {
                        RunningPortfolio.AssetBalance = balances.Available;
                    }
                }

                successful = true;
            }

            return successful;
        }

        public override async Task<bool> UpdatePriceIntervalsAsync()
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

        public override async Task<bool> AttemptToInitiateTradeAsync()
        {
            //TODO: Temporary for testing.
            //  Will need to answer whether I should sell, buy, or hold?

            await Task.Delay(5000);
            TradeOrder? tradeOrder = _tradeAnalyzerService.AnalyzePotentialTrade();

            if (tradeOrder != null)
            {
                if (tradeOrder.TransactionType == TradeOrder.TransactionTypes.Buy && _exchangeService.Buy(tradeOrder))
                {
                    _dataAccessor.Create(tradeOrder);
                    RunningPortfolio.LastCompletedTradeOrder = tradeOrder;
                }
                else if (tradeOrder.TransactionType == TradeOrder.TransactionTypes.Sell && _exchangeService.Sell(tradeOrder))
                {
                    _dataAccessor.Create(tradeOrder);
                    RunningPortfolio.LastCompletedTradeOrder = tradeOrder;
                }


                //await UpdateBalancesAsync();
            }

            return true;
        }

        protected override async Task ScheduleJobsAsync()
        {
            #region BitcoinPriceIntervalsJob
            IJobDetail bitcoinPriceIntervalsJob = JobBuilder.Create<BitcoinPriceIntervalsJob>()
                .WithIdentity(nameof(BitcoinPriceIntervalsJob), nameof(BitcoinBotTraderService))
                .Build();

            ITrigger bitcoinPriceIntervalsTrigger = TriggerBuilder.Create()
                .WithIdentity($"{nameof(BitcoinPriceIntervalsJob)}Trigger", nameof(BitcoinBotTraderService))
                .StartAt(new DateTimeOffset(DateTime.Now.Date.AddDays(1)))
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(24)
                    .RepeatForever())
                .Build();

            await _scheduler.ScheduleJob(bitcoinPriceIntervalsJob, bitcoinPriceIntervalsTrigger);
            #endregion

            #region AttemptToInitiateTradeJob
            IJobDetail attemptToInitiateTradeJob = JobBuilder.Create<AttemptToInitiateTradeJob>()
                .WithIdentity(nameof(AttemptToInitiateTradeJob), nameof(BitcoinBotTraderService))
                .Build();

            ITrigger attemptToInitiateTradeTrigger = TriggerBuilder.Create()
                .WithIdentity($"{nameof(AttemptToInitiateTradeJob)}Trigger", nameof(BitcoinBotTraderService))
                .StartAt(new DateTimeOffset(DateTime.Now.AddSeconds(MinSecondsBetweenTrades)))
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(MinSecondsBetweenTrades)
                    .RepeatForever())
                .Build();

            await _scheduler.ScheduleJob(attemptToInitiateTradeJob, attemptToInitiateTradeTrigger);
            #endregion

            await _scheduler.Start();
        }

        public override async Task WriteTradeOrdersToCSVAsync()
        {
            IOrderedEnumerable<TradeOrder> trades = _dataAccessor
                .ReadAll<TradeOrder>()
                .OrderByDescending(to => to.TimeStamp);

            await IO.WriteToCSVAsync(trades, _logger);
        }
    }
}
