using System;

namespace CaptaTrader.Business.Modules.Trading.ApplicationServices;

public interface ITradingService
{
    Task InitializeTradingAsync();
    Task ShutdownTradingAsync();
}
