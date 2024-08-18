using CaptaTrader.Core.Base.Extensions;
using CaptaTrader.Core.Jobs;
using CaptaTrader.Domains;
using CaptaTrader.Services;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Core
{
    public class BitcoinTradingBot
    {
        private const string BITCOIN_SYMBOL = "BTC";
        private const decimal TRADING_FEE_SPOT = 0.001M;
        private const decimal TRADING_FEE_INSTANT = 0.005M;

        private readonly ILogger _logger;

        public ICryptoDataService CryptoDataService { get; set; }
        public IExchangeService ExchangeService { get; set; }
        public PriceInterval PriceIntervalMonth { get; set; }
        public PriceInterval PriceIntervalDay { get; set; }
        public decimal AccountBalance { get; set; }
        public decimal BitcoinBalance { get; set; }
        public decimal AveragePricePaid { get; set; }

        public BitcoinTradingBot(ICryptoDataService cryptoDataService, IExchangeService exchangeService, ILogger logger)
        {
            CryptoDataService = cryptoDataService;
            ExchangeService = exchangeService;
            _logger = logger;

            AccountBalance = ExchangeService.GetAccountBalance() ?? 0.0M;
            BitcoinBalance = ExchangeService.GetAssetBalance(BITCOIN_SYMBOL) ?? 0.0M;

            Task.Run(() => UpdateBitcoinPriceIntervalsAsync()).Wait();

            Task.Run(() => ScheduleJobsAsync());
        }

        private async Task ScheduleJobsAsync()
        {
            StdSchedulerFactory factory = new StdSchedulerFactory();
            IScheduler scheduler = await factory.GetScheduler();

            await scheduler.Start();

            scheduler.Context.Put(nameof(BitcoinTradingBot), this);

            IJobDetail bitcoinPriceIntervalsJob = JobBuilder.Create<BitcoinPriceIntervalsJob>()
                .WithIdentity(nameof(BitcoinPriceIntervalsJob), nameof(BitcoinTradingBot))
                .Build();

            ITrigger bitcoinPriceIntervalsTrigger = TriggerBuilder.Create()
                .WithIdentity($"{nameof(BitcoinPriceIntervalsJob)}Trigger", nameof(BitcoinTradingBot))
                .StartAt(new DateTimeOffset(DateTime.Now.Date.AddDays(1)))
                .WithSimpleSchedule(x => x
                    .WithIntervalInHours(24)
                    .RepeatForever())
                .Build();

            await scheduler.ScheduleJob(bitcoinPriceIntervalsJob, bitcoinPriceIntervalsTrigger);
        }

        public async Task UpdateBitcoinPriceIntervalsAsync()
        {
            DateTime dateTimeTodayStart = DateTime.Now.Date;

            PriceIntervalMonth = await CryptoDataService.GetPricesOverInterval(BITCOIN_SYMBOL, dateTimeTodayStart.AddMonths(-1), dateTimeTodayStart);

            await Task.Delay(TimeSpan.FromSeconds(CryptoDataService.GetMinimumSecondsBetweenCalls()));

            PriceIntervalDay = await CryptoDataService.GetPricesOverInterval(BITCOIN_SYMBOL, dateTimeTodayStart.AddDays(-1), dateTimeTodayStart);
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

        //TODO: Instead of figuring cost/sale in real money, need to get cost AND fee into real money.
        //TODO: Set cost on transactions.

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"Account Balance: {AccountBalance}");
            builder.AppendLine($"Bitcoin Balance: {BitcoinBalance}");
            builder.Append(Environment.NewLine);

            if (PriceIntervalMonth != null)
            {
                builder.AppendLine($"Bitcoin Last Month: {PriceIntervalMonth.Direction} {PriceIntervalMonth.Change}%");
                builder.AppendLine($"Bitcoin Last Day: {PriceIntervalDay.Direction} {PriceIntervalDay.Change}%");
            }
            else
            {
                builder.AppendLine($"No Bitcoin price interval data.");
            }

            builder.Append(Environment.NewLine);

            return builder.ToString();
        }

        private ExchangeTransaction LookToBuy()
        {
            // if im holding USD, look for a price to buy (should either be when down or when a signifigant climb is predicted)
            if (AccountBalance > 0M)
            {
                //TODO: Get best price of BTC. Also factor in the quanity may be less than I could buy.
                Thread.Sleep(1000);
                decimal priceBitcoin = new decimal(new Random().NextDouble() * (40000 - 20000) + 20000);

                if (priceBitcoin > 0M)//TODO: Pick a good time to buy.
                {
                    decimal amountOfBitcoinICanAfford = AccountBalance / priceBitcoin;
                    decimal costOfMaxAmountOfBitcoin = amountOfBitcoinICanAfford * priceBitcoin;
                    // Take out fee from max cost, to make sure I can cover the most it could be.
                    costOfMaxAmountOfBitcoin -= (costOfMaxAmountOfBitcoin * TRADING_FEE_SPOT);
                    decimal costInRealMoney = costOfMaxAmountOfBitcoin.PrecisionTruncate(2);
                    decimal bitcoinToBuy = costInRealMoney / priceBitcoin;

                    if (bitcoinToBuy > 0M)
                    {
                        //TODO: If transaction was successful.
                        decimal fee = costInRealMoney * TRADING_FEE_SPOT;
                        decimal costPlusFee = costInRealMoney + fee;
                        decimal previousTotalCost = BitcoinBalance * AveragePricePaid;
                        decimal newTotalCost = previousTotalCost + costPlusFee;
                        decimal newTotalBitcoinBalance = BitcoinBalance + bitcoinToBuy;
                        AveragePricePaid = newTotalCost / newTotalBitcoinBalance;
                        BitcoinBalance += newTotalBitcoinBalance;
                        AccountBalance -= costPlusFee;

                        return new ExchangeTransaction
                        {
                            Type = ExchangeTransaction.Types.Buy,
                            Date = DateTime.Now,
                            Quantity = bitcoinToBuy,
                            Price = priceBitcoin,
                            Fee = fee,
                            Total = costPlusFee
                        };
                    }
                }
            }

            return null;
        }

        private ExchangeTransaction LookToSell()
        {
            //if im holding BTC, look for a price to sell it, should be as soon as profit will be double the fee
            if (BitcoinBalance > 0M)
            {
                //TODO: Get best sell price of BTC. Also factor in the quantity may be less than I could sell.
                Thread.Sleep(1000);
                decimal priceBitcoin = new decimal(new Random().NextDouble() * (40000 - 20000) + 20000);

                if (priceBitcoin > AveragePricePaid) //TODO: and the fee is no more than half the difference.
                {
                    decimal maxUnits = BitcoinBalance; //TODO: Replace with actual quantity;
                    decimal maxSale = maxUnits * priceBitcoin;
                    decimal saleInRealMoney = maxSale.PrecisionTruncate(2);
                    decimal unitsToSell = saleInRealMoney / priceBitcoin;
                    decimal totalPaid = unitsToSell * AveragePricePaid;
                    decimal fee = saleInRealMoney * TRADING_FEE_SPOT;

                    bool isProfitable = (saleInRealMoney - totalPaid) >= (fee * 2);

                    if (isProfitable)
                    {
                        //TODO: If transaction was successful.
                        decimal saleMinusFee = saleInRealMoney - fee;
                        AccountBalance += saleMinusFee;
                        BitcoinBalance -= unitsToSell;

                        return new ExchangeTransaction
                        {
                            Type = ExchangeTransaction.Types.Sell,
                            Date = DateTime.Now,
                            Quantity = unitsToSell,
                            Price = priceBitcoin,
                            Fee = fee,
                            Total = saleMinusFee
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
    }
}
