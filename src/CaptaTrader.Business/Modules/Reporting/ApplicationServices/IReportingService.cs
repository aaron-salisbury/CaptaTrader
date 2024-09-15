using System;

namespace CaptaTrader.Business.Modules.Reporting.ApplicationServices;

public interface IReportingService
{
    Task WriteTradeOrdersAsync();
}
