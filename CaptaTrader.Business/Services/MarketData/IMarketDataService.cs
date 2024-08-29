using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services
{
    internal interface IMarketDataService
    {
        Task<IEnumerable<PriceInterval>?> GetLastThirtyDaysOfPriceIntervals(string exchangeID, string baseAsset);
    }
}
