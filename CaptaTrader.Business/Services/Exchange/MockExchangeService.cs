using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services
{
    public class MockExchangeService : IExchangeService
    {
        private readonly Random _random = new();

        private Dictionary<string, AssetBalance> _balancesByAssets = new()
        { 
            { "USDT", new AssetBalance()
                {
                    Asset = "USDT",
                    Available = 250000.00M,
                    Locked = 0.0M,
                    Total = 250000.00M
                }
            } 
        };

        public string GetExchangeID() => "MockExchange";

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
                        decimal balance = _random.NextDecimal(0.001M, 0.02M).PrecisionTruncate(3);
                        balancesForAsset = new AssetBalance()
                        {
                            Asset = uppercaseAsset,
                            Available = balance,
                            Locked = 0.0M,
                            Total = balance
                        };
                        _balancesByAssets[uppercaseAsset] = balancesForAsset;
                    }

                    assetBalances.Add(balancesForAsset);
                }
            });

            return assetBalances;
        }

        public async Task<decimal?> GetSpotPriceAsync(string symbol, string quoteAsset)
        {
            decimal price = await Task.Run(() => { return _random.NextDecimal(55000.00M, 60000.00M).PrecisionTruncate(2); });
            
            return price;
        }

        public bool Buy(string symbol, decimal quantity, decimal price)
        {
            decimal totalPrice = quantity * price;
            AssetBalance usd = _balancesByAssets["USDT"];

            if (usd.Available >= totalPrice && _balancesByAssets.TryGetValue(symbol.ToUpper(), out AssetBalance? asset))
            {
                usd.Available -= totalPrice;
                asset.Available += quantity;

                return true;
            }

            return false;
        }

        public bool Sell(string symbol, decimal quantity, decimal price)
        {
            decimal totalPrice = quantity * price;
            AssetBalance usd = _balancesByAssets["USDT"];

            if (_balancesByAssets.TryGetValue(symbol.ToUpper(), out AssetBalance? asset) && asset.Available >= quantity)
            {
                usd.Available += totalPrice;
                asset.Available -= quantity;

                return true;
            }

            return false;
        }
    }
}
