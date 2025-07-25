using System;

namespace CaptaTrader.Business.Modules.Trading.DTOs;

public record CryptoAssetDto
{
    public required string Symbol { get; set; }
    public required decimal Amount { get; set; }
    public required decimal AveragePriceUSDT { get; set; }
}
