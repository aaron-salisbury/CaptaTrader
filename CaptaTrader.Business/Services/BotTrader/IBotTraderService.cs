using CaptaTrader.Business.DTOs;

namespace CaptaTrader.Business.Services
{
    public interface IBotTraderService
    {
        event EventHandler<TradeEventArgs>? TradeCompleted;

        Task<bool> InitializeAsync();
        Task ShutdownAsync();
        Task<bool> UpdateBalancesAsync();
        Task<bool> UpdatePriceIntervalsAsync();
        Task<bool> AttemptToInitiateTradeAsync();
    }

    public class TradeEventArgs(Trade tradeOrder) : EventArgs
    {
        internal Trade TradeOrder { get; set; } = tradeOrder;
    }
}
