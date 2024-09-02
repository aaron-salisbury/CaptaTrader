using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services
{
    public interface IBotTraderService
    {
        Portfolio RunningPortfolio { get; set; }

        Task<bool> InitializeAsync();
        Task ShutdownAsync();
        Task<bool> UpdateBalancesAsync();
        Task<bool> UpdatePriceIntervalsAsync();
        Task<bool> AttemptToInitiateTradeAsync();
        Task WriteTradeOrdersToCSVAsync();
    }
}
