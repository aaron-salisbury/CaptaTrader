using CaptaTrader.Business.DTOs;

namespace CaptaTrader.Business.Services;

public interface IMarketDataService
{
    Task<IEnumerable<PriceInterval>?> GetLastThirtyDaysOfPriceIntervals(string exchangeID, string baseAsset);
}
