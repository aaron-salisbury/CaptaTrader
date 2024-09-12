using System;

namespace CaptaTrader.Business.Services.Domain;

public interface IReportingService
{
    Task WriteTradeOrdersAsync();
}
