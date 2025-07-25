using System;

namespace CaptaTrader.Business.Modules.Trading.DTOs;

public record TradeOrderDto
{
    public enum TransactionTypes
    {
        Buy,
        Sell
    }

    public required DateTime TimeStamp { get; init; }

    public required TransactionTypes TransactionType { get; init; }

    public required string AssetSymbolBuying { get; init; }

    public required string AssetSymbolSpending { get; init; }

    public required decimal Price { get; init; }

    public required decimal Amount { get; init; }
}
