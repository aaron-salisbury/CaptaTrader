using CaptaTrader.Business.DTOs;
using CaptaTrader.Business.Modules.Trading.DTOs;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices;

public interface IExchangeService
{
    string GetExchangeID();
    Task<IEnumerable<AssetBalance>?> GetAccountBalancesAsync(IEnumerable<string> assets);
    Task<decimal?> GetSpotPriceAsync(string symbol, string quoteAsset);
    bool Buy(TradeOrderDto tradeOrder);
    bool Sell(TradeOrderDto tradeOrder);
}
