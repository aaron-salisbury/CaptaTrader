using System;

namespace CaptaTrader.Business.Modules.Trading.ApplicationServices;

public interface IReportingService
{
    Task WriteTradeOrdersAsync();
}
