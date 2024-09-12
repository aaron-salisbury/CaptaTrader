using CaptaTrader.Data;
using CaptaTrader.Data.Base;
using CaptaTrader.Domains;
using Microsoft.Extensions.Logging;

namespace CaptaTrader.Business.Services.Domain;

public class ReportingService : IReportingService
{
    private readonly IDataAccess _dataAccess;
    private readonly ILogger _logger;
    private readonly ISessionValueProvider _sessionValueProvider;

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
            IEnumerable<ExecutedTrade> thisUsersTrades = _dataAccess.ReadFiltered<ExecutedTrade>(et => et.EndUserId == sessionUserId)
                .OrderByDescending(to => to.TimeStamp);

            await IO.WriteToCSVAsync(thisUsersTrades, _logger);
        }
    }
}
