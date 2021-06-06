using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.SpotData;
using CaptaTrader.Domains;
using CaptaTrader.Services.Base.Extensions;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using CryptoExchange.Net.Objects;

namespace CaptaTrader.Services.TradingBots
{
    public class BinanceTradingBotService : ITradingBotService
    {
        // https://github.com/JKorf/Binance.Net
        // https://github.com/JKorf/Binance.Net/blob/master/Examples/ClientConsole/Program.cs

        private const string ENDPOINT = "https://api.binance.us";
        //TODO: RIGHT NOW REPOSITORY IS PRIVATE. BEFORE MAKING PUBLIC GET NEW KEYS AND DELETE THESE!!!
        private const string API_KEY = "Z1m1ZhRsRSm6IJTmI5rfXaWIc9Y3AE7CZfrCkL2BWxKI6LwCXz8LbkqPunISoFUj";
        private const string SECRET_KEY = "tJ6Hg4O6hEIKhdPIYmx4AC4rFEKBc4r4TNkTbgQHuGhXMvAEjIFmVYpRks7HTWCj";
        private const decimal TRADING_FEE_SPOT = 0.001M;
        private const decimal TRADING_FEE_INSTANT = 0.005M;

        public decimal AccountBalance { get; set; }

        public decimal BitcoinBalance { get; set; }

        //public decimal Paid { get; set; }

        //public decimal AveragePricePaid => Paid / BitcoinBalance;

        public decimal AveragePricePaid { get; set; }

        public BinanceTradingBotService()
        {
            BinanceClient.SetDefaultOptions(new BinanceClientOptions()
            {
                BaseAddress = ENDPOINT,



                ApiCredentials = new ApiCredentials(API_KEY, SECRET_KEY),
                //LogVerbosity = LogVerbosity.Debug,
                //LogWriters = new List<TextWriter> { Console.Out }
            });

            BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
            {
                ApiCredentials = new ApiCredentials(API_KEY, SECRET_KEY),
                LogVerbosity = LogVerbosity.Debug,
                LogWriters = new List<TextWriter> { Console.Out }
            });
        }

        public IEnumerable<string> ActivateAutoTrading()
        {
            yield return "Bot is active.";

            AccountBalance = GetAccountBalance();
            yield return $"Account Balance: {AccountBalance}";

            BitcoinBalance = GetAssetBalance("BTC");
            yield return $"Bitcoin Balance: {BitcoinBalance}";

            //TODO: Also need quantity with the price.
            //decimal bitcoinSpotPrice = GetSpotPrice("BTCUSDT");


            while(true)
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

        public decimal GetAccountBalance()
        {
            using (var client = new BinanceClient())
            {
                WebCallResult<BinanceAccountInfo> result = client.General.GetAccountInfo();

                return result.Data.Balances.Where(b => string.Equals(b.Asset, "USD")).FirstOrDefault().Free;
            }
        }

        public decimal GetAssetBalance(string symbol)
        {
            using (var client = new BinanceClient())
            {
                WebCallResult<BinanceAccountInfo> result = client.General.GetAccountInfo();

                return result.Data.Balances.Where(b => string.Equals(b.Asset, symbol)).FirstOrDefault().Total;
            }
        }

        public decimal GetSpotPrice(string symbol)
        {
            using (var client = new BinanceClient())
            {
                WebCallResult<BinanceBookPrice> result = client.Spot.Market.GetBookPrice(symbol);

                return result.Data.BestAskPrice;
            }
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
                    costOfMaxAmountOfBitcoin = costOfMaxAmountOfBitcoin - (costOfMaxAmountOfBitcoin * TRADING_FEE_SPOT);
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
