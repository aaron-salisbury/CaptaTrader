using CaptaTrader.Business.Modules.Trading.DTOs;

namespace CaptaTrader.Business.Modules.Trading.MessageContracts;

public record TradeExecuted()
{
    public required TradeOrderDto TradeOrder { get; init; }
}
