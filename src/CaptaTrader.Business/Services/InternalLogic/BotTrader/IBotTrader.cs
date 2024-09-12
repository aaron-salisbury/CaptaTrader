using CaptaTrader.Business.DTOs;

namespace CaptaTrader.Business.Services;

public interface IBotTrader
{
    Task<bool> UpdateBalancesAsync();
    Task<bool> UpdatePriceIntervalsAsync();
    Task<TradeOrder?> AttemptToInitiateTradeAsync();
    Task WriteTradeOrdersToCSVAsync();
}
