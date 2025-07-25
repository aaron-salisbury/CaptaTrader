using System;

namespace CaptaTrader.Business.Modules.Trading.DTOs;

public record AssetBalance
{
    public required string Asset { get; init; }
    public decimal AvailableBalance { get; init; }
    public decimal UnavailableBalance { get; init; }
}
