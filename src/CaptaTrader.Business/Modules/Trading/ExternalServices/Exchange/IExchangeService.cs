using CaptaTrader.Business.Modules.Trading.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices;

internal interface IExchangeService
{
    string GetExchangeID();
    string GetExchangeMediumSymbol();
    Task<IEnumerable<AssetBalance>?> GetAccountBalancesAsync(IEnumerable<string> assets);
    Task<decimal?> GetSpotPriceAsync(string symbol);
    Task<DateTime?> BuyAsync(TradeOrderDto tradeOrder);
    Task<DateTime?> SellAsync(TradeOrderDto tradeOrder);
}
