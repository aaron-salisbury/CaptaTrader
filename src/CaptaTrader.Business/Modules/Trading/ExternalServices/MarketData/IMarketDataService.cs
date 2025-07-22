using CaptaTrader.Business.Modules.Trading.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices;

public interface IMarketDataService
{
    Task<IEnumerable<PriceInterval>?> GetLastThirtyDaysOfPriceIntervals(string exchangeID, string baseAsset);
}
