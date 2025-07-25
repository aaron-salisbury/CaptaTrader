using CaptaTrader.Business.Modules.Trading.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.DomainServices;

internal interface IBotTrader
{
    Task<IEnumerable<TradeOrderDto>> AttemptToInitiateTradesAsync();
}
