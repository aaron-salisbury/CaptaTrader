using CaptaTrader.Business.Modules.Trading.DTOs;

namespace CaptaTrader.Business.Modules.Trading.DomainServices.BotTrader;

public interface IBotTrader
{
    Task<bool> UpdateBalancesAsync();
    Task<bool> UpdatePriceIntervalsAsync();
    Task<TradeOrderDto?> AttemptToInitiateTradeAsync();
}
