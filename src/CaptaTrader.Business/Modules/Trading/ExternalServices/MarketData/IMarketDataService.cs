using CaptaTrader.Business.DTOs;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices.MarketData;

public interface IMarketDataService
{
    Task<IEnumerable<PriceInterval>?> GetLastThirtyDaysOfPriceIntervals(string exchangeID, string baseAsset);
}
