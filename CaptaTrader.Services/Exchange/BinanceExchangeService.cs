using Binance.Net;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.SpotData;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using CryptoExchange.Net.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CaptaTrader.Services
{
    // API Examples: https://github.com/JKorf/Binance.Net/blob/master/Examples/ClientConsole/Program.cs
    public class BinanceExchangeService : IExchangeService
    {
        private const string ENDPOINT = "https://api.binance.us";

        public BinanceExchangeService(string apiKey, string secretApiKey)
        {
            BinanceClient.SetDefaultOptions(new BinanceClientOptions()
            {
                BaseAddress = ENDPOINT,



                ApiCredentials = new ApiCredentials(apiKey, secretApiKey),
                //LogVerbosity = LogVerbosity.Debug,
                //LogWriters = new List<TextWriter> { Console.Out }
            });

            BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiKey, secretApiKey),
                LogVerbosity = LogVerbosity.Debug,
                LogWriters = new List<TextWriter> { Console.Out }
            });
        }

        public decimal GetAccountBalance()
        {
            using (BinanceClient client = new BinanceClient())
            {
                WebCallResult<BinanceAccountInfo> result = client.General.GetAccountInfo();

                return result.Data.Balances.Where(b => string.Equals(b.Asset, "USD")).FirstOrDefault().Free;
            }
        }

        public decimal GetAssetBalance(string symbol)
        {
            using (BinanceClient client = new BinanceClient())
            {
                WebCallResult<BinanceAccountInfo> result = client.General.GetAccountInfo();

                return result.Data.Balances.Where(b => string.Equals(b.Asset, symbol)).FirstOrDefault().Total;
            }
        }

        public decimal GetSpotPrice(string symbol)
        {
            using (BinanceClient client = new BinanceClient())
            {
                WebCallResult<BinanceBookPrice> result = client.Spot.Market.GetBookPrice(symbol);

                return result.Data.BestAskPrice;
            }
        }
    }
}
