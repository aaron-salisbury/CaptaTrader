using System;

namespace CaptaTrader.Business.Modules.Trading.DTOs;

public record AssetBalance
{
    public required string Asset { get; set; }
    public decimal Available { get; set; }
    public decimal Locked { get; set; }
    public decimal Total { get; set; }
}
