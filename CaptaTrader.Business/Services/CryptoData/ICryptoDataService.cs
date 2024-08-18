using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services
{
    public interface ICryptoDataService
    {
        int GetMinimumSecondsBetweenCalls();

        Task<PriceInterval?> GetPricesOverInterval(string symbol, DateTime start, DateTime end);
    }
}
