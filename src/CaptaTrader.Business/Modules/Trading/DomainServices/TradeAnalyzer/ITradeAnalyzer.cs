using CaptaTrader.Business.Modules.Trading.DTOs;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.DomainServices;

internal interface ITradeAnalyzer
{
    Task<TradeOrderDto?> AnalyzePotentialBuyAsync(string symbolOfAssetToBuy, AssetBalance currencyAsset, decimal spotPrice);

    Task<TradeOrderDto?> AnalyzePotentialSellAsync(AssetBalance assetToSell, string symbolOfCurrencyAsset, decimal spotPrice);
}
