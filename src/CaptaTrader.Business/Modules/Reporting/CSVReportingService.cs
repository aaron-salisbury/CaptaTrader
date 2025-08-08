using CaptaTrader.Business.Modules.Trading.DTOs;
using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using CaptaTrader.Data.Base;
using Microsoft.Extensions.Logging;
using RunnethOverStudio.AppToolkit.Modules.DataAccess;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules;

internal sealed class CSVReportingService : IReportingService
{
    private readonly ISQLDataAccess _dataAccess;
    private readonly IUserAccessService _userAccessService;
    private readonly ILogger _logger;

    public CSVReportingService(ISQLDataAccess dataAccess, IUserAccessService userAccessService, ILogger logger)
    {
        _dataAccess = dataAccess;
        _userAccessService = userAccessService;
        _logger = logger;
    }

    public async Task WriteTradeOrdersAsync()
    {
        long? sessionUserId = _userAccessService.GetSessionEndUserId();

        if (sessionUserId != null)
        {
            IEnumerable<TradeOrder> usersTrades = (await _dataAccess.ReadAsync<TradeOrder>("EndUserId = @userId", new { userId = sessionUserId.Value }))
                .OrderByDescending(to => to.TimeStamp);

            //TODO: will need to make a mapper (ClassMap<TradeOrderDto>) and pass it in. One thing for sure to do is convert price to two decimal places since just one looks weird for a price.
            await IO.WriteToCSVAsync(usersTrades, _logger);
        }
    }
}
