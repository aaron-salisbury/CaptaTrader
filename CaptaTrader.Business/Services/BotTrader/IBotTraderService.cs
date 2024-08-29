using CaptaTrader.Business.DTOs;

namespace CaptaTrader.Business.Services
{
    internal interface IBotTraderService
    {
        event EventHandler<TradeEventArgs>? TradeCompleted;

        Task<bool> InitializeAsync();
        Task ShutdownAsync();
        Task<bool> UpdateBalancesAsync();
        Task<bool> UpdatePriceIntervalsAsync();
        Task<bool> AttemptToInitiateTradeAsync();
    }

    internal class TradeEventArgs(Trade tradeOrder) : EventArgs
    {
        internal Trade TradeOrder { get; set; } = tradeOrder;
    }
}
