// Ignore Spelling: Bitcoin crypto

using CaptaTrader.Business.Jobs;
using CaptaTrader.Business.Models;
using CaptaTrader.Models;
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
        private readonly ICryptoDataService _cryptoDataService;
        private readonly IExchangeService _exchangeService;
        private readonly ITradeAnalyzerService _tradeAnalyzerService;

        public BitcoinBotTraderService(ICryptoDataService cryptoDataService, IExchangeService exchangeService, ITradeAnalyzerService tradeAnalyzerService, ILogger logger, Action<Notification> notifyAction) : base(notifyAction)
        {
            _cryptoDataService = cryptoDataService;
            _exchangeService = exchangeService;
            _tradeAnalyzerService = tradeAnalyzerService;
            _logger = logger;
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
            IEnumerable<AccountBalances>? accountAssetBalances = await _exchangeService.GetAccountBalancesAsync(new List<string>() { QUOTE_ASSET, BITCOIN_SYMBOL });

            if (accountAssetBalances != null)
            {
                foreach (AccountBalances balances in accountAssetBalances)
                {
                    if (string.Equals(balances.Asset, QUOTE_ASSET))
                    {
                        AccountBalance = balances.Available;
                    }
                    else if (string.Equals(balances.Asset, BITCOIN_SYMBOL))
                    {
                        AssetBalance = balances.Available;
                    }
                }

                successful = true;
            }

            return successful;
        }

        public override async Task<bool> UpdatePriceIntervalsAsync()
        {
            bool successful = false;
            IEnumerable<PriceInterval>? priceIntervals = await _cryptoDataService.GetLastThirtyDaysOfPriceIntervals();

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
            //TODO: Answer whether I should I sell, buy, or hold?

            //TODO: TESTING
            await Task.Delay(5000);
            TradeTransaction? tradeTransaction = _tradeAnalyzerService.AnalyzePotentialTrade();

            if (tradeTransaction != null)
            {
                NotifyOfTransaction(tradeTransaction);

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




        //public IEnumerable<string> Activate()
        //{
        //    //TODO: Instead of yield returning strings, take advantage of Notification

        //    yield return "Bot is active.";
        //    yield return string.Empty;

        //    //TODO: Also need quantity with the price.
        //    //decimal bitcoinSpotPrice = GetSpotPrice("BTCUSDT");

        //    while (true) //TODO: Replace true with some sort of class level cancellation token
        //    {
        //        ExchangeTransaction? buyTransaction = LookToBuy();
        //        if (buyTransaction != null)
        //        {
        //            Task.Run(() => WriteTransactionToFile(buyTransaction));

        //            yield return buyTransaction.ToString();
        //            yield return $"Account Balance: {AccountBalance}";
        //            yield return $"Bitcoin Balance: {AssetBalance}";
        //            yield return $"Average Price: {AveragePricePaid}";
        //            yield return string.Empty;
        //        }

        //        ExchangeTransaction? sellTransaction = LookToSell();
        //        if (sellTransaction != null)
        //        {
        //            Task.Run(() => WriteTransactionToFile(sellTransaction));

        //            yield return sellTransaction.ToString();
        //            yield return $"Account Balance: {AccountBalance}";
        //            yield return $"Bitcoin Balance: {AssetBalance}";
        //            yield return $"Average Price: {AveragePricePaid}";
        //            yield return string.Empty;
        //        }
        //    }
        //}

        ////TODO: Instead of figuring cost/sale in real money, need to get cost AND fee into real money.
        ////TODO: Set cost on transactions.

        //private ExchangeTransaction? LookToBuy()
        //{
        //    // if im holding USD, look for a price to buy (should either be when down or when a signifigant climb is predicted)
        //    if (AccountBalance != null && AccountBalance > 0M && AssetBalance != null)
        //    {
        //        //TODO: Get best price of BTC. Also factor in the quanity may be less than I could buy.
        //        Thread.Sleep(1000);
        //        decimal priceBitcoin = new decimal(new Random().NextDouble() * (40000 - 20000) + 20000);
        //        //TODO: This is just a test:
        //        //decimal? priceBitcoin = await _exchangeService.GetSpotPriceAsync(BITCOIN_SYMBOL, QUOTE_ASSET);
        //        //_logger.LogDebug($"BTC Spot: {priceBitcoin}");

        //        if (priceBitcoin > 0M)//TODO: Pick a good time to buy.
        //        {
        //            decimal amountOfBitcoinICanAfford = AccountBalance.Value / priceBitcoin;
        //            decimal costOfMaxAmountOfBitcoin = amountOfBitcoinICanAfford * priceBitcoin;
        //            // Take out fee from max cost, to make sure I can cover the most it could be.
        //            costOfMaxAmountOfBitcoin -= costOfMaxAmountOfBitcoin * TRADING_FEE_SPOT;
        //            decimal costInRealMoney = costOfMaxAmountOfBitcoin.PrecisionTruncate(2);
        //            decimal bitcoinToBuy = costInRealMoney / priceBitcoin;

        //            if (bitcoinToBuy > 0M)
        //            {
        //                //TODO: If transaction was successful.
        //                decimal fee = costInRealMoney * TRADING_FEE_SPOT;
        //                decimal costPlusFee = costInRealMoney + fee;
        //                decimal previousTotalCost = AssetBalance.Value * AveragePricePaid;
        //                decimal newTotalCost = previousTotalCost + costPlusFee;
        //                decimal newTotalBitcoinBalance = AssetBalance.Value + bitcoinToBuy;
        //                AveragePricePaid = newTotalCost / newTotalBitcoinBalance;
        //                AssetBalance += newTotalBitcoinBalance;
        //                AccountBalance -= costPlusFee;

        //                return new ExchangeTransaction
        //                {
        //                    Symbol = BITCOIN_SYMBOL,
        //                    Type = ExchangeTransaction.Types.Buy,
        //                    Date = DateTime.Now,
        //                    Quantity = bitcoinToBuy,
        //                    Price = priceBitcoin,
        //                    Cost = costInRealMoney,
        //                    Fee = fee,
        //                    Total = bitcoinToBuy * costPlusFee
        //                };
        //            }
        //        }
        //    }

        //    return null;
        //}

        //private ExchangeTransaction? LookToSell()
        //{
        //    //if im holding BTC, look for a price to sell it, should be as soon as profit will be double the fee
        //    if (AssetBalance != null && AssetBalance > 0M)
        //    {
        //        //TODO: Get best sell price of BTC. Also factor in the quantity may be less than I could sell.
        //        Thread.Sleep(1000);
        //        decimal priceBitcoin = new decimal(new Random().NextDouble() * (40000 - 20000) + 20000);

        //        if (priceBitcoin > AveragePricePaid) //TODO: and the fee is no more than half the difference.
        //        {
        //            decimal maxUnits = AssetBalance.Value; //TODO: Replace with actual quantity;
        //            decimal maxSale = maxUnits * priceBitcoin;
        //            decimal saleInRealMoney = maxSale.PrecisionTruncate(2);
        //            decimal unitsToSell = saleInRealMoney / priceBitcoin;
        //            decimal totalPaid = unitsToSell * AveragePricePaid;
        //            decimal fee = saleInRealMoney * TRADING_FEE_SPOT;

        //            bool isProfitable = saleInRealMoney - totalPaid >= fee * 2; //TODO: Double seems arbitrary. Come up with real threshold.

        //            if (isProfitable)
        //            {
        //                //TODO: If transaction was successful.
        //                decimal saleMinusFee = saleInRealMoney - fee;
        //                AccountBalance += saleMinusFee;
        //                AssetBalance -= unitsToSell;

        //                return new ExchangeTransaction
        //                {
        //                    Symbol = BITCOIN_SYMBOL,
        //                    Type = ExchangeTransaction.Types.Sell,
        //                    Date = DateTime.Now,
        //                    Quantity = unitsToSell,
        //                    Price = priceBitcoin,
        //                    Fee = fee,
        //                    Total = unitsToSell * priceBitcoin + fee
        //                };
        //            }
        //        }
        //    }

        //    return null;
        //}

        //private async Task WriteTransactionToFile(ExchangeTransaction exchangeTransaction)
        //{
        //    if (exchangeTransaction != null)
        //    {
        //        try
        //        {
        //            //TODO: Write to a CSV instead.
        //            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        //            string appFolderPath = Path.Combine(documentsPath, AppDomain.CurrentDomain.FriendlyName);

        //            if (!Directory.Exists(appFolderPath))
        //            {
        //                Directory.CreateDirectory(appFolderPath);
        //            }

        //            string filename = $"Transactions_{exchangeTransaction.Date.Year}.txt";
        //            string filePath = Path.Combine(appFolderPath, filename);

        //            using (StreamWriter writer = File.AppendText(filePath))
        //            {
        //                await writer.WriteLineAsync(exchangeTransaction.ToString());
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(default, ex, "Failed to write transaction to file.");
        //        }
        //    }
        //}
    }
}
