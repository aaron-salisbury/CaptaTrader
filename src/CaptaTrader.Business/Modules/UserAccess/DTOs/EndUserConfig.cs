using System;

namespace CaptaTrader.Business.Modules.UserAccess.DTOs
{
    public record EndUserConfig
    {
        public enum MarketDataAPIs
        {
            CoinAPI
        }

        public enum ExchangeAPIs
        {
            Binance

        }

        public long EndUserConfigId { get; init; }

        public long EndUserId { get; init; }

        public string? Email { get; set; }

        public MarketDataAPIs? MarketDataAPI { get; set; }

        public string? MarketDataAPIKey { get; set; }

        public ExchangeAPIs? ExchangeAPI { get; set; }

        public string? ExchangeAPIKey { get; set; }

        public string? ExchangeSecretKey { get; set; }
    }
}
