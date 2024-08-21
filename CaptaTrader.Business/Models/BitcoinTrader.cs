// Ignore Spelling: Bitcoin crypto

using CaptaTrader.Business.Base;
using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Jobs;
using CaptaTrader.Business.Services;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using static CaptaTrader.Business.Models.Notification;

namespace CaptaTrader.Business.Models
{
    public class BitcoinTrader : ObservableModel
    {
        private const string BITCOIN_SYMBOL = "BTC";
        private const string QUOTE_ASSET = "USDT";
        private const decimal TRADING_FEE_SPOT = 0.001M;
        private const decimal TRADING_FEE_INSTANT = 0.005M;

        public PriceInterval? PriceIntervalMonth { get; set; }
        public PriceInterval? PriceIntervalDay { get; set; }
        public decimal? AccountBalance { get; set; }
        public decimal? BitcoinBalance { get; set; }
        public decimal AveragePricePaid { get; set; } = 1.0M; //TODO: This is wrong, need to initialize this with what is real, where I am the balances.

        private Notification? _lastNotification;
        public Notification? LastNotification
        {
            get => _lastNotification;
            set => SetProperty(ref _lastNotification, value);
        }

        private readonly ILogger _logger;
        private readonly ICryptoDataService _cryptoDataService;
        private readonly IExchangeService _exchangeService;
        private readonly IScheduler _scheduler;

        public BitcoinTrader(ICryptoDataService cryptoDataService, IExchangeService exchangeService, ILogger logger)
        {
            _cryptoDataService = cryptoDataService;
            _exchangeService = exchangeService;
            _logger = logger;
            _scheduler = Task.Run(() => new StdSchedulerFactory().GetScheduler()).Result;
        }

        public void Initialize()
        {
            UpdateBalancesAsync().Wait();

            UpdatePriceIntervalsAsync().Wait();

            //await ScheduleJobsAsync();

            NotifyStatus();
        }

        public async Task UpdateBalancesAsync()
        {
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
                        BitcoinBalance = balances.Available;
                    }
                }
            }
        }

        public async Task UpdatePriceIntervalsAsync()
        {
            IEnumerable<PriceInterval>? priceIntervals = await _cryptoDataService.GetOneMonthPriceIntervals();

            DateTime dateTimeTodayStart = DateTime.Now.Date;
            if (priceIntervals != null)
            {
                PriceIntervalDay = priceIntervals.Where(pi => pi.StartDate.Date == dateTimeTodayStart).FirstOrDefault();

                if (PriceIntervalDay == null)
                {
                    dateTimeTodayStart.AddDays(-1);
                    PriceIntervalDay = priceIntervals.Where(pi => pi.StartDate.Date == dateTimeTodayStart).FirstOrDefault();
                }

                PriceIntervalMonth = priceIntervals.Where(pi => pi.StartDate.Date == dateTimeTodayStart.AddMonths(-1)).FirstOrDefault();
            }
        }

        public IEnumerable<string> Activate()
        {
            //TODO: Instead of yield returning strings, take advantage of Notification

            yield return "Bot is active.";
            yield return string.Empty;

            //TODO: Also need quantity with the price.
            //decimal bitcoinSpotPrice = GetSpotPrice("BTCUSDT");

            while (true) //TODO: Replace true with some sort of class level cancellation token
            {
                ExchangeTransaction? buyTransaction = LookToBuy();
                if (buyTransaction != null)
                {
                    Task.Run(() => WriteTransactionToFile(buyTransaction));

                    yield return buyTransaction.ToString();
                    yield return $"Account Balance: {AccountBalance}";
                    yield return $"Bitcoin Balance: {BitcoinBalance}";
                    yield return $"Average Price: {AveragePricePaid}";
                    yield return string.Empty;
                }

                ExchangeTransaction? sellTransaction = LookToSell();
                if (sellTransaction != null)
                {
                    Task.Run(() => WriteTransactionToFile(sellTransaction));

                    yield return sellTransaction.ToString();
                    yield return $"Account Balance: {AccountBalance}";
                    yield return $"Bitcoin Balance: {BitcoinBalance}";
                    yield return $"Average Price: {AveragePricePaid}";
                    yield return string.Empty;
                }
            }
        }

        private async Task ScheduleJobsAsync()
        {
            _scheduler.Context.Put(nameof(BitcoinTrader), this);

            IJobDetail bitcoinPriceIntervalsJob = JobBuilder.Create<BitcoinPriceIntervalsJob>()
                .WithIdentity(nameof(BitcoinPriceIntervalsJob), nameof(BitcoinTrader))
                .Build();

            ITrigger bitcoinPriceIntervalsTrigger = TriggerBuilder.Create()
                .WithIdentity($"{nameof(BitcoinPriceIntervalsJob)}Trigger", nameof(BitcoinTrader))
                .StartAt(new DateTimeOffset(DateTime.Now.Date.AddDays(1)))
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(24)
                    .RepeatForever())
                .Build();

            await _scheduler.ScheduleJob(bitcoinPriceIntervalsJob, bitcoinPriceIntervalsTrigger);

            await _scheduler.Start();
        }

        private async Task UnscheduleJobsAsync()
        {
            await _scheduler.Clear();

            await _scheduler.Standby();
        }

        private void NotifyStatus()
        {
            LastNotification = new Notification()
            {
                Title = "Bot Status",
                NotificationType = NotificationTypes.Table,
                CellContentsByColumnNames = new Dictionary<string, string>()
                {
                    { "Account Balance", AccountBalance?.ToString() ?? string.Empty },
                    { "Bitcoin Balance", BitcoinBalance?.ToString() ?? string.Empty },
                    { "Bitcoin Last Month", PriceIntervalMonth != null
                        ? $"{PriceIntervalMonth?.Direction} {PriceIntervalMonth?.Change}%"
                        : string.Empty },
                    { "Bitcoin Last Day", PriceIntervalDay != null
                    ? $"{PriceIntervalDay?.Direction} {PriceIntervalDay?.Change}%"
                    : string.Empty }
                }
            };
        }

        //TODO: Instead of figuring cost/sale in real money, need to get cost AND fee into real money.
        //TODO: Set cost on transactions.

        private ExchangeTransaction? LookToBuy()
        {
            // if im holding USD, look for a price to buy (should either be when down or when a signifigant climb is predicted)
            if (AccountBalance != null && AccountBalance > 0M && BitcoinBalance != null)
            {
                //TODO: Get best price of BTC. Also factor in the quanity may be less than I could buy.
                Thread.Sleep(1000);
                decimal priceBitcoin = new decimal(new Random().NextDouble() * (40000 - 20000) + 20000);
                //TODO: This is just a test:
                //decimal? priceBitcoin = await _exchangeService.GetSpotPriceAsync(BITCOIN_SYMBOL, QUOTE_ASSET);
                //_logger.LogDebug($"BTC Spot: {priceBitcoin}");

                if (priceBitcoin > 0M)//TODO: Pick a good time to buy.
                {
                    decimal amountOfBitcoinICanAfford = AccountBalance.Value / priceBitcoin;
                    decimal costOfMaxAmountOfBitcoin = amountOfBitcoinICanAfford * priceBitcoin;
                    // Take out fee from max cost, to make sure I can cover the most it could be.
                    costOfMaxAmountOfBitcoin -= costOfMaxAmountOfBitcoin * TRADING_FEE_SPOT;
                    decimal costInRealMoney = costOfMaxAmountOfBitcoin.PrecisionTruncate(2);
                    decimal bitcoinToBuy = costInRealMoney / priceBitcoin;

                    if (bitcoinToBuy > 0M)
                    {
                        //TODO: If transaction was successful.
                        decimal fee = costInRealMoney * TRADING_FEE_SPOT;
                        decimal costPlusFee = costInRealMoney + fee;
                        decimal previousTotalCost = BitcoinBalance.Value * AveragePricePaid;
                        decimal newTotalCost = previousTotalCost + costPlusFee;
                        decimal newTotalBitcoinBalance = BitcoinBalance.Value + bitcoinToBuy;
                        AveragePricePaid = newTotalCost / newTotalBitcoinBalance;
                        BitcoinBalance += newTotalBitcoinBalance;
                        AccountBalance -= costPlusFee;

                        return new ExchangeTransaction
                        {
                            Symbol = BITCOIN_SYMBOL,
                            Type = ExchangeTransaction.Types.Buy,
                            Date = DateTime.Now,
                            Quantity = bitcoinToBuy,
                            Price = priceBitcoin,
                            Cost = costInRealMoney,
                            Fee = fee,
                            Total = bitcoinToBuy * costPlusFee
                        };
                    }
                }
            }

            return null;
        }

        private ExchangeTransaction? LookToSell()
        {
            //if im holding BTC, look for a price to sell it, should be as soon as profit will be double the fee
            if (BitcoinBalance != null && BitcoinBalance > 0M)
            {
                //TODO: Get best sell price of BTC. Also factor in the quantity may be less than I could sell.
                Thread.Sleep(1000);
                decimal priceBitcoin = new decimal(new Random().NextDouble() * (40000 - 20000) + 20000);

                if (priceBitcoin > AveragePricePaid) //TODO: and the fee is no more than half the difference.
                {
                    decimal maxUnits = BitcoinBalance.Value; //TODO: Replace with actual quantity;
                    decimal maxSale = maxUnits * priceBitcoin;
                    decimal saleInRealMoney = maxSale.PrecisionTruncate(2);
                    decimal unitsToSell = saleInRealMoney / priceBitcoin;
                    decimal totalPaid = unitsToSell * AveragePricePaid;
                    decimal fee = saleInRealMoney * TRADING_FEE_SPOT;

                    bool isProfitable = saleInRealMoney - totalPaid >= fee * 2; //TODO: Double seems arbitrary. Come up with real threshold.

                    if (isProfitable)
                    {
                        //TODO: If transaction was successful.
                        decimal saleMinusFee = saleInRealMoney - fee;
                        AccountBalance += saleMinusFee;
                        BitcoinBalance -= unitsToSell;

                        return new ExchangeTransaction
                        {
                            Symbol = BITCOIN_SYMBOL,
                            Type = ExchangeTransaction.Types.Sell,
                            Date = DateTime.Now,
                            Quantity = unitsToSell,
                            Price = priceBitcoin,
                            Fee = fee,
                            Total = unitsToSell * priceBitcoin + fee
                        };
                    }
                }
            }

            return null;
        }

        private async Task WriteTransactionToFile(ExchangeTransaction exchangeTransaction)
        {
            if (exchangeTransaction != null)
            {
                try
                {
                    //TODO: Write to a CSV instead.
                    string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    string appFolderPath = Path.Combine(documentsPath, AppDomain.CurrentDomain.FriendlyName);

                    if (!Directory.Exists(appFolderPath))
                    {
                        Directory.CreateDirectory(appFolderPath);
                    }

                    string filename = $"Transactions_{exchangeTransaction.Date.Year}.txt";
                    string filePath = Path.Combine(appFolderPath, filename);

                    using (StreamWriter writer = File.AppendText(filePath))
                    {
                        await writer.WriteLineAsync(exchangeTransaction.ToString());
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(default, ex, "Failed to write transaction to file.");
                }
            }
        }

        //public override string ToString()
        //{
        //    StringBuilder builder = new();
        //    builder.AppendLine("*** Bitcoin Trading Bot Status ***");
        //    builder.AppendLine($"   Account Balance: {AccountBalance}");
        //    builder.AppendLine($"   Bitcoin Balance: {BitcoinBalance}");

        //    if (PriceIntervalMonth != null && PriceIntervalDay != null)
        //    {
        //        builder.AppendLine($"   Bitcoin Last Month: {PriceIntervalMonth.Direction} {PriceIntervalMonth.Change}%");
        //        builder.AppendLine($"   Bitcoin Last Day: {PriceIntervalDay.Direction} {PriceIntervalDay.Change}%");
        //    }
        //    else
        //    {
        //        builder.AppendLine($"   No price interval data.");
        //    }

        //    builder.Append(Environment.NewLine);

        //    return builder.ToString();
        //}
    }
}
