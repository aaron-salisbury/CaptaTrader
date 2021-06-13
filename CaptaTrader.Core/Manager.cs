using CaptaTrader.Services;
using System.Collections.Generic;

namespace CaptaTrader.Core
{
    public class Manager
    {
        private const string NOMICS_API_KEY = "your-nomics-api-key-here";
        private const string BINANCE_API_KEY = "your-binance-api-key-here";
        private const string BINANCE_SECRET_API_KEY = "your-binance-secret-api-key-here";

        public BitcoinTradingBot BitcoinTradingBot { get; set; }

        public Manager()
        {
            //TODO: pass real logger, not null
            ICryptoDataService cryptoDataService = new NomicsCryptoDataService(null, NOMICS_API_KEY);
            IExchangeService exchangeService = new BinanceExchangeService(BINANCE_API_KEY, BINANCE_SECRET_API_KEY);

            BitcoinTradingBot = new BitcoinTradingBot(cryptoDataService, exchangeService);
        }

        public IEnumerable<string> ActivateAutoTrading()
        {
            foreach (string response in BitcoinTradingBot.Activate())
            {
                yield return response;
            }
        }
    }
}
