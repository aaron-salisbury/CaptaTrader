using CaptaTrader.Core.Base.Extensions;
using CaptaTrader.Domains;
using CaptaTrader.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Core
{
    public class BitcoinTradingBot
    {
        private const string BITCOIN_SYMBOL = "BTC";
        private const decimal TRADING_FEE_SPOT = 0.001M;
        private const decimal TRADING_FEE_INSTANT = 0.005M;

        public ICryptoDataService CryptoDataService { get; set; }

        public IExchangeService ExchangeService { get; set; }

        public PriceInterval PriceIntervalMonth { get; set; }

        public PriceInterval PriceIntervalDay { get; set; }

        public decimal AccountBalance { get; set; }

        public decimal BitcoinBalance { get; set; }

        public decimal AveragePricePaid { get; set; }

        public BitcoinTradingBot(ICryptoDataService cryptoDataService, IExchangeService exchangeService)
        {
            CryptoDataService = cryptoDataService;
            ExchangeService = exchangeService;

            DateTime dateTimeTodayStart = DateTime.Now.Date;
            //TODO: Update every midnight.
            PriceIntervalMonth = Task.Run(() => CryptoDataService.GetPricesOverInterval(BITCOIN_SYMBOL, dateTimeTodayStart.AddMonths(-1), dateTimeTodayStart)).Result;
            PriceIntervalDay = Task.Run(() => CryptoDataService.GetPricesOverInterval(BITCOIN_SYMBOL, dateTimeTodayStart.AddDays(-1), dateTimeTodayStart)).Result;

            AccountBalance = ExchangeService.GetAccountBalance();
            BitcoinBalance = ExchangeService.GetAssetBalance(BITCOIN_SYMBOL);
        }

        public IEnumerable<string> Activate()
        {
            yield return "Bot is active.";
            yield return Environment.NewLine;

            yield return $"Account Balance: {AccountBalance}";
            yield return $"Bitcoin Balance: {BitcoinBalance}";
            yield return Environment.NewLine;

            //TODO: Also need quantity with the price.
            //decimal bitcoinSpotPrice = GetSpotPrice("BTCUSDT");

            while (true)
            {
                ExchangeTransaction buyTransaction = LookToBuy();
                if (buyTransaction != null)
                {
                    yield return buyTransaction.ToString();
                    yield return $"Account Balance: {AccountBalance}";
                    yield return $"Bitcoin Balance: {BitcoinBalance}";
                    yield return $"Average Price: {AveragePricePaid}";
                    yield return Environment.NewLine;
                }

                ExchangeTransaction sellTransaction = LookToSell();
                if (sellTransaction != null)
                {
                    yield return sellTransaction.ToString();
                    yield return $"Account Balance: {AccountBalance}";
                    yield return $"Bitcoin Balance: {BitcoinBalance}";
                    yield return $"Average Price: {AveragePricePaid}";
                    yield return Environment.NewLine;
                }
            }

            //if im holding BTC, look for a price to sell it, should be as soon as profit will be double the fee

            // if im holding USD, look for a price to buy (should either be when down or when a signifigant climb is predicted)
        }

        //TODO: Instead of figuring cost/sale in real money, need to get cost AND fee into real money.

        private ExchangeTransaction LookToBuy()
        {
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

                        //TODO: Write to permanent log.
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
            if (BitcoinBalance > 0M)
            {
                //TODO: Get best sell price of BTC. Also factor in the quanity may be less than I could sell.
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

                        //TODO: Write to permanent log.
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
    }
}
