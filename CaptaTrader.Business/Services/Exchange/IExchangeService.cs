using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services
{
    public interface IExchangeService
    {
        Task<IEnumerable<AccountBalances>?> GetAccountBalancesAsync(IEnumerable<string> assets);
        Task<decimal?> GetSpotPriceAsync(string symbol, string quoteAsset);
        bool Buy(string symbol, decimal quantity, decimal price);
        bool Sell(string symbol, decimal quantity, decimal price);
    }
}
