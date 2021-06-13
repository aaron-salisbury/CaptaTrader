using CaptaTrader.Domains;
using System;
using System.Threading.Tasks;

namespace CaptaTrader.Services
{
    public interface ICryptoDataService
    {
        Task<PriceInterval> GetPricesOverInterval(string symbol, DateTime start, DateTime end);
    }
}
