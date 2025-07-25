using CaptaTrader.Business.Modules.Trading;
using CaptaTrader.Business.Modules.Trading.DTOs;
using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using CaptaTrader.Data.Base;
using CaptaTrader.Data.Entities;
using CaptaTrader.Data.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules;

internal sealed class CSVReportingService : IReportingService
{
    private readonly IDataAccess _dataAccess;
    private readonly IUserAccessService _userAccessService;
    private readonly ILogger _logger;
    private readonly TradingMapper _tradingMapper = new();

    public CSVReportingService(IDataAccess dataAccess, IUserAccessService userAccessService, ILogger logger)
    {
        _dataAccess = dataAccess;
        _userAccessService = userAccessService;
        _logger = logger;
    }

    public async Task WriteTradeOrdersAsync()
    {
        int? sessionUserId = _userAccessService.GetSessionEndUserId();

        if (sessionUserId != null)
        {
            IEnumerable<TradeOrderDto> thisUsersTrades = _dataAccess.ReadFiltered<TradeOrder>(et => et.EndUserId == sessionUserId)
                .OrderByDescending(to => to.TimeStamp)
                .Select(_tradingMapper.MapToDto<TradeOrderDto>);

            //TODO: will need to make a mapper and pass it in.
            await IO.WriteToCSVAsync(thisUsersTrades, _logger);
        }
    }
}
