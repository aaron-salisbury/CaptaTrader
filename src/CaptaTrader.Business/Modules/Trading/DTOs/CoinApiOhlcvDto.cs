using System.Text.Json.Serialization;

namespace CaptaTrader.Business.Modules.Trading.DTOs;

public record CoinApiOhlcvDto
{
    [JsonPropertyName("time_period_start")]
    public DateTime TimePeriodStart { get; init; }

    [JsonPropertyName("time_period_end")]
    public DateTime TimePeriodEnd { get; init; }

    [JsonPropertyName("time_open")]
    public DateTime TimeOpen { get; init; }

    [JsonPropertyName("time_close")]
    public DateTime TimeClose { get; init; }

    [JsonPropertyName("price_open")]
    public double PriceOpen { get; init; }

    [JsonPropertyName("price_high")]
    public double PriceHigh { get; init; }

    [JsonPropertyName("price_low")]
    public double PriceLow { get; init; }

    [JsonPropertyName("price_close")]
    public double PriceClose { get; init; }

    [JsonPropertyName("volume_traded")]
    public double VolumeTraded { get; init; }

    [JsonPropertyName("trades_count")]
    public int TradesCount { get; init; }
}
