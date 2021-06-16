using CaptaTrader.Domains;
using System;
using System.Threading.Tasks;

namespace CaptaTrader.Services
{
    public interface ICryptoDataService
    {
        int GetMinimumSecondsBetweenCalls();

        Task<PriceInterval> GetPricesOverInterval(string symbol, DateTime start, DateTime end);
    }
}
