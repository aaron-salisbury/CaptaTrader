using System;

namespace CaptaTrader.Business.Services
{
    public interface IBotTraderService
    {
        Task<bool> InitializeAsync();
        Task ShutdownAsync();
        Task<bool> UpdateBalancesAsync();
        Task<bool> UpdatePriceIntervalsAsync();
        Task<bool> AttemptToInitiateTradeAsync();
    }
}
