using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.SpotData;
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
    }
}
