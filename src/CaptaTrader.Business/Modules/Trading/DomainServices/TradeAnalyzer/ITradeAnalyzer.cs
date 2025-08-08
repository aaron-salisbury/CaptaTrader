using CaptaTrader.Business.Modules.Trading.DTOs;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.DomainServices;

internal interface ITradeAnalyzer
{
    Task<TradeOrder?> AnalyzePotentialBuyAsync(long userId, string symbolOfAssetToBuy, AssetBalance currencyAsset, decimal spotPrice);

    Task<TradeOrder?> AnalyzePotentialSellAsync(long userId, AssetBalance assetToSell, string symbolOfCurrencyAsset, decimal spotPrice);
}
