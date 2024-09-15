using CaptaTrader.Business.Modules.Trading;
using CaptaTrader.Business.Modules.Trading.DTOs;
using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using CaptaTrader.Data;
using CaptaTrader.Data.Base;
using CaptaTrader.Data.Entities;
using Microsoft.Extensions.Logging;

namespace CaptaTrader.Business.Modules.Reporting.ApplicationServices;

public class ReportingService : IReportingService
{
    private readonly IDataAccess _dataAccess;
    private readonly ILogger _logger;
    private readonly ISessionValueProvider _sessionValueProvider;
    private readonly TradingMapper _tradingMapper = new();

    public ReportingService(IDataAccess dataAccess, ILogger logger, ISessionValueProvider sessionValueProvider)
    {
        _dataAccess = dataAccess;
        _logger = logger;
        _sessionValueProvider = sessionValueProvider;
    }

    public async Task WriteTradeOrdersAsync()
    {
        int? sessionUserId = _sessionValueProvider.GetUserID();

        if (sessionUserId != null)
        {
            IEnumerable<TradeOrderDto> thisUsersTrades = _dataAccess.ReadFiltered<TradeOrder>(et => et.EndUserId == sessionUserId)
                .OrderByDescending(to => to.TimeStamp)
                .Select(_tradingMapper.MapToDto<TradeOrderDto>);

            await IO.WriteToCSVAsync(thisUsersTrades, _logger);
        }
    }
}
