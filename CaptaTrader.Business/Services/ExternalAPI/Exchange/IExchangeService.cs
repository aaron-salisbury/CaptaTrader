using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services
{
    public interface IExchangeService
    {
        string GetExchangeID();
        Task<IEnumerable<AssetBalance>?> GetAccountBalancesAsync(IEnumerable<string> assets);
        Task<decimal?> GetSpotPriceAsync(string symbol, string quoteAsset);
        bool Buy(TradeOrder tradeOrder);
        bool Sell(TradeOrder tradeOrder);
    }
}
