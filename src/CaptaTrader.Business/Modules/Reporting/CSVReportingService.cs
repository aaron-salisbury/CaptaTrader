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

            //TODO: will need to make a mapper (ClassMap<TradeOrderDto>) and pass it in. One thing for sure to do is convert price to two decimal places since just one looks weird for a price.
            await IO.WriteToCSVAsync(thisUsersTrades, _logger);
        }
    }
}
