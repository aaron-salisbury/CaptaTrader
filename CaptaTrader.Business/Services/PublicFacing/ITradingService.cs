using System;

namespace CaptaTrader.Business.Services.Domain
{
    public interface ITradingService
    {
        Task InitializeTradingAsync();
        Task ShutdownTradingAsync();
    }
}
