using CaptaTrader.Business.Modules.Trading.DTOs;
using RunnethOverStudio.AppToolkit.Core.Extensions;
using System;
using System.Threading.Tasks;
using static CaptaTrader.Business.Modules.Trading.DTOs.TradeOrderDto;

namespace CaptaTrader.Business.Modules.Trading.DomainServices;

internal sealed class MockTradeAnalyzer : ITradeAnalyzer
{
    public Task<TradeOrderDto?> AnalyzePotentialBuyAsync(string symbolOfAssetToBuy, AssetBalance currencyAsset, decimal spotPrice)
    {
        TransactionTypes transactionType = TransactionTypes.Buy;
        string symbolSpending = currencyAsset.Asset.ToUpperInvariant();

        TradeOrderDto? tradeOrder = CreateMockTradeTransaction(currencyAsset, transactionType, spotPrice, symbolOfAssetToBuy, symbolSpending);

        return Task.FromResult(tradeOrder);
    }

    public Task<TradeOrderDto?> AnalyzePotentialSellAsync(AssetBalance assetToSell, string symbolOfCurrencyAsset, decimal spotPrice)
    {
        TransactionTypes transactionType = TransactionTypes.Sell;
        string symbolSpending = assetToSell.Asset.ToUpperInvariant();

        TradeOrderDto? tradeOrder = CreateMockTradeTransaction(assetToSell, transactionType, spotPrice, symbolOfCurrencyAsset, symbolSpending);

        return Task.FromResult(tradeOrder);
    }

    private static TradeOrderDto? CreateMockTradeTransaction(AssetBalance asset, TransactionTypes transactionType, decimal spotPrice, string symbolBuying, string symbolSpending)
    {
        // Simulate a third of the time a profit not being made, so no trade.
        if (Random.Shared.Next(3) == 0)
        {
            return null;
        }

        // Use mock price to calculate quantity based on available asset balance.
        decimal quantity;
        if (transactionType == TransactionTypes.Sell)
        {
            // If selling, sell all we have, capped to eight decimals.
            quantity = asset.AvailableBalance.PrecisionTruncate(8);
        }
        else
        {
            // If buying, spend all the currency we have, capped to two decimals and ensuring we don't spend less than one unit.
            quantity = (asset.AvailableBalance.PrecisionTruncate(2) / spotPrice).PrecisionTruncate(8);
            if (quantity * spotPrice < 1.0M)
            {
                quantity = 0.0M;
            }
        }

        // Skip trade if not enough balance.
        if (quantity <= 0.0M)
        {
            return null;
        }

        return new TradeOrderDto()
        {
            TimeStamp = DateTime.Now,
            TransactionType = transactionType,
            AssetSymbolBuying = symbolBuying,
            AssetSymbolSpending = symbolSpending,
            Price = spotPrice,
            Amount = quantity
        };
    }
}
