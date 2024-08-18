// Ignore Spelling: Bitcoin crypto

using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Jobs;
using CaptaTrader.Business.Models;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using System.Text;

namespace CaptaTrader.Business.Services
{
    public class BitcoinTradingService : ITradingService
    {
        private const string BITCOIN_SYMBOL = "BTC";
        private const decimal TRADING_FEE_SPOT = 0.001M;
        private const decimal TRADING_FEE_INSTANT = 0.005M;

        public PriceInterval? PriceIntervalMonth { get; set; }
        public PriceInterval? PriceIntervalDay { get; set; }
        public decimal? AccountBalance { get; set; }
        public decimal? BitcoinBalance { get; set; }
        public decimal AveragePricePaid { get; set; } = 1.0M; //TODO: This is wrong, need to initialize this with what is real, where I am the balances.

        private readonly ILogger _logger;
        private readonly ICryptoDataService _cryptoDataService;
        private readonly IExchangeService _exchangeService;

        public BitcoinTradingService(ICryptoDataService cryptoDataService, IExchangeService exchangeService, ILogger logger)
        {
            _cryptoDataService = cryptoDataService;
            _exchangeService = exchangeService;
            _logger = logger;
        }

        public async Task Initialize()
        {
            AccountBalance = _exchangeService.GetAccountBalance();
            BitcoinBalance = _exchangeService.GetAssetBalance(BITCOIN_SYMBOL);

            UpdatePriceIntervalsAsync().Wait();

            await ScheduleJobsAsync();
        }

        public async Task UpdatePriceIntervalsAsync()
        {
            DateTime dateTimeTodayStart = DateTime.Now.Date;

            PriceIntervalMonth = await _cryptoDataService.GetPricesOverInterval(BITCOIN_SYMBOL, dateTimeTodayStart.AddMonths(-1), dateTimeTodayStart);

            await Task.Delay(TimeSpan.FromSeconds(_cryptoDataService.GetMinimumSecondsBetweenCalls()));

            PriceIntervalDay = await _cryptoDataService.GetPricesOverInterval(BITCOIN_SYMBOL, dateTimeTodayStart.AddDays(-1), dateTimeTodayStart);
        }

        public IEnumerable<string> Activate()
        {
            yield return "Bot is active.";
            yield return string.Empty;

            //TODO: Also need quantity with the price.
            //decimal bitcoinSpotPrice = GetSpotPrice("BTCUSDT");

            while (true) //TODO: Replace true with some sort of class level cancellation token
            {
                ExchangeTransaction buyTransaction = LookToBuy();
                if (buyTransaction != null)
                {
                    Task.Run(() => WriteTransactionToFile(buyTransaction));

                    yield return buyTransaction.ToString();
                    yield return $"Account Balance: {AccountBalance}";
                    yield return $"Bitcoin Balance: {BitcoinBalance}";
                    yield return $"Average Price: {AveragePricePaid}";
                    yield return string.Empty;
                }

                ExchangeTransaction sellTransaction = LookToSell();
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
            StdSchedulerFactory factory = new();
            IScheduler scheduler = await factory.GetScheduler();

            await scheduler.Start();

            scheduler.Context.Put(nameof(BitcoinTradingService), this);

            IJobDetail bitcoinPriceIntervalsJob = JobBuilder.Create<BitcoinPriceIntervalsJob>()
                .WithIdentity(nameof(BitcoinPriceIntervalsJob), nameof(BitcoinTradingService))
                .Build();

            ITrigger bitcoinPriceIntervalsTrigger = TriggerBuilder.Create()
                .WithIdentity($"{nameof(BitcoinPriceIntervalsJob)}Trigger", nameof(BitcoinTradingService))
                .StartAt(new DateTimeOffset(DateTime.Now.Date.AddDays(1)))
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(24)
                    .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(bitcoinPriceIntervalsJob, bitcoinPriceIntervalsTrigger);
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
                catch (Exception e)
                {
                    _logger.LogError($"Failed to write transaction to file: {e.Message}", exchangeTransaction);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new();
            builder.AppendLine("*** Bitcoin Trading Bot Status ***");
            builder.AppendLine($"   Account Balance: {AccountBalance}");
            builder.AppendLine($"   Bitcoin Balance: {BitcoinBalance}");

            if (PriceIntervalMonth != null && PriceIntervalDay != null)
            {
                builder.AppendLine($"   Bitcoin Last Month: {PriceIntervalMonth.Direction} {PriceIntervalMonth.Change}%");
                builder.AppendLine($"   Bitcoin Last Day: {PriceIntervalDay.Direction} {PriceIntervalDay.Change}%");
            }
            else
            {
                builder.AppendLine($"   No price interval data.");
            }

            builder.Append(Environment.NewLine);

            return builder.ToString();
        }
    }
}
