using CaptaTrader.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace CaptaTrader.Core
{
    public class Manager
    {
        private const string NOMICS_API_KEY = "your-nomics-api-key-here";
        private const string BINANCE_API_KEY = "your-binance-api-key-here";
        private const string BINANCE_SECRET_API_KEY = "your-binance-secret-api-key-here";

        private readonly ILogger _logger;

        public BitcoinTradingBot BitcoinTradingBot { get; set; }

        public Manager(ILogger logger)
        {
            _logger = logger;

            ICryptoDataService cryptoDataService = new NomicsCryptoDataService(_logger, NOMICS_API_KEY);
            IExchangeService exchangeService = new BinanceExchangeService(_logger, BINANCE_API_KEY, BINANCE_SECRET_API_KEY);

            BitcoinTradingBot = new BitcoinTradingBot(cryptoDataService, exchangeService, _logger);
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
