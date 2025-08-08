using System;

namespace CaptaTrader.Business.Modules.Trading.DTOs;

public record TradeOrder
{
    public enum TransactionTypes
    {
        Buy,
        Sell
    }

    public required long EndUserId { get; init; }

    public DateTime? TimeStamp { get; set; } // Not set until actual trade execution.

    public required TransactionTypes TransactionType { get; init; }

    public required string AssetSymbolBuying { get; init; }

    public required string AssetSymbolSpending { get; init; }

    public required decimal Price { get; init; }

    public required decimal Amount { get; init; }
}
