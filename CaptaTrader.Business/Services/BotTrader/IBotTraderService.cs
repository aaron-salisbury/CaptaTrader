using CaptaTrader.Business.Entities;

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

    public class TradeEventArgs(TradeOrder tradeOrder) : EventArgs
    {
        internal TradeOrder TradeOrder { get; set; } = tradeOrder;
    }
}
