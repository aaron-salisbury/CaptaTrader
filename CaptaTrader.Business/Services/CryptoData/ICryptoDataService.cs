using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services
{
    public interface ICryptoDataService
    {
        Task<IEnumerable<PriceInterval>?> GetOneMonthPriceIntervals();
    }
}
