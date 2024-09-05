using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services.Domain
{
    public interface ITradingService
    {
        Portfolio RunningPortfolio { get; set; }

        Task<bool> InitializeTradingAsync();
        Task ShutdownTradingAsync();
    }
}
