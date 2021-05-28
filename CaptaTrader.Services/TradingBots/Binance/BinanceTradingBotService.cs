using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects;
using Binance.Net.Objects.Spot;
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
                //client.Spot.Market.GetBookPrice("BTCUSDT");
                //client.General.GetAccountInfo();
                WebCallResult<BinanceAccountInfo> info = client.General.GetAccountInfo();

                return info.Data.Balances.Where(b => string.Equals(b.Asset, "USD")).FirstOrDefault().Free;
            }
        }
    }
}
