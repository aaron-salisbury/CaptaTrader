// Ignore Spelling: Ohlcv Api

using System.Text.Json.Serialization;

namespace CaptaTrader.Business.Services.DTOs
{
    [Serializable]
    public class CoinApiOhlcv
    {
        [JsonPropertyName("time_period_start")]
        public DateTime TimePeriodStart { get; set; }

        [JsonPropertyName("time_period_end")]
        public DateTime TimePeriodEnd { get; set; }

        [JsonPropertyName("time_open")]
        public DateTime TimeOpen { get; set; }

        [JsonPropertyName("time_close")]
        public DateTime TimeClose { get; set; }

        [JsonPropertyName("price_open")]
        public double PriceOpen { get; set; }

        [JsonPropertyName("price_high")]
        public double PriceHigh { get; set; }

        [JsonPropertyName("price_low")]
        public double PriceLow { get; set; }

        [JsonPropertyName("price_close")]
        public double PriceClose { get; set; }

        [JsonPropertyName("volume_traded")]
        public double VolumeTraded { get; set; }

        [JsonPropertyName("trades_count")]
        public int TradesCount { get; set; }
    }
}
