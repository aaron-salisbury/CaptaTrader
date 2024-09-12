using CaptaTrader.Business.DTOs;

namespace CaptaTrader.Business.MessageContracts;

public record TradeExecuted()
{
    public required TradeOrder TradeOrder { get; init; }
}
