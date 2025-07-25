using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Modules.Trading.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices;

internal sealed class MockExchangeService : IExchangeService
{
    private const string EXCHANGE_MEDIUM_SYMBOL = "USDT";

    private readonly Dictionary<string, AssetBalance> _balancesByAssets = new(2);

    public MockExchangeService()
    {
        _balancesByAssets[EXCHANGE_MEDIUM_SYMBOL] = new AssetBalance()
        {
            Asset = EXCHANGE_MEDIUM_SYMBOL,
            AvailableBalance = 250000.00M,
            UnavailableBalance = 0.0M
        };
    }

    public string GetExchangeID() => "MockExchange";

    public string GetExchangeMediumSymbol() => EXCHANGE_MEDIUM_SYMBOL;

    public async Task<IEnumerable<AssetBalance>?> GetAccountBalancesAsync(IEnumerable<string> assets)
    {
        List<AssetBalance> assetBalances = [];

        await Task.Run(() =>
        {
            foreach (string asset in assets)
            {
                string uppercaseAsset = asset.ToUpper();
                AssetBalance balancesForAsset;

                if (_balancesByAssets.TryGetValue(uppercaseAsset, out AssetBalance? existingAssetBalance))
                {
                    balancesForAsset = existingAssetBalance;
                }
                else
                {
                    balancesForAsset = new AssetBalance()
                    {
                        Asset = uppercaseAsset,
                        AvailableBalance = 0.0M,
                        UnavailableBalance = 0.0M
                    };
                    _balancesByAssets[uppercaseAsset] = balancesForAsset;
                }

                assetBalances.Add(balancesForAsset);
            }
        });

        return assetBalances;
    }

    public async Task<decimal?> GetSpotPriceAsync(string symbol)
    {
        decimal price = await Task.Run(() => { return Random.Shared.NextDecimal(55000.00M, 60000.00M).PrecisionTruncate(2); });

        return price;
    }

    public async Task<DateTime?> BuyAsync(TradeOrderDto tradeOrder)
    {
        DateTime transactionTimeStamp = await Task.Run(() => 
        {
            _balancesByAssets[tradeOrder.AssetSymbolBuying] = new AssetBalance() // Getting Crypto
            {
                Asset = _balancesByAssets[tradeOrder.AssetSymbolBuying].Asset,
                AvailableBalance = _balancesByAssets[tradeOrder.AssetSymbolBuying].AvailableBalance + tradeOrder.Amount,
                UnavailableBalance = _balancesByAssets[tradeOrder.AssetSymbolBuying].UnavailableBalance
            };

            _balancesByAssets[tradeOrder.AssetSymbolSpending] = new AssetBalance() // Spending Exchange Medium
            {
                Asset = _balancesByAssets[tradeOrder.AssetSymbolSpending].Asset,
                AvailableBalance = _balancesByAssets[tradeOrder.AssetSymbolSpending].AvailableBalance - (tradeOrder.Amount * tradeOrder.Price),
                UnavailableBalance = _balancesByAssets[tradeOrder.AssetSymbolSpending].UnavailableBalance
            };

            return DateTime.Now;
        });

        return transactionTimeStamp;
    }

    public async Task<DateTime?> SellAsync(TradeOrderDto tradeOrder)
    {
        DateTime transactionTimeStamp = await Task.Run(() =>
        {
            _balancesByAssets[tradeOrder.AssetSymbolBuying] = new AssetBalance() // Getting Exchange Medium
            {
                Asset = _balancesByAssets[tradeOrder.AssetSymbolBuying].Asset,
                AvailableBalance = _balancesByAssets[tradeOrder.AssetSymbolBuying].AvailableBalance + (tradeOrder.Amount * tradeOrder.Price),
                UnavailableBalance = _balancesByAssets[tradeOrder.AssetSymbolBuying].UnavailableBalance
            };

            _balancesByAssets[tradeOrder.AssetSymbolSpending] = new AssetBalance() // Selling Crypto
            {
                Asset = _balancesByAssets[tradeOrder.AssetSymbolSpending].Asset,
                AvailableBalance = _balancesByAssets[tradeOrder.AssetSymbolSpending].AvailableBalance - tradeOrder.Amount,
                UnavailableBalance = _balancesByAssets[tradeOrder.AssetSymbolSpending].UnavailableBalance
            };

            return DateTime.Now;
        });

        return transactionTimeStamp;
    }
}
