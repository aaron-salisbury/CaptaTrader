using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services
{
    internal class MockExchangeService : IExchangeService
    {
        private readonly Random _random = new();

        private Dictionary<string, AccountBalances> _balancesByAssets = new()
        { 
            { "USDT", new AccountBalances()
                {
                    Asset = "USDT",
                    Available = 250000.00M,
                    Locked = 0.0M,
                    Total = 250000.00M
                }
            } 
        };

        public string GetExchangeID() => "MockExchange";

        public async Task<IEnumerable<AccountBalances>?> GetAccountBalancesAsync(IEnumerable<string> assets)
        {
            List<AccountBalances> accountBalances = [];

            await Task.Run(() =>
            {
                foreach (string asset in assets)
                {
                    string uppercaseAsset = asset.ToUpper();
                    AccountBalances balancesForAsset;

                    if (_balancesByAssets.TryGetValue(uppercaseAsset, out AccountBalances? accountBalanceForAsset))
                    {
                        balancesForAsset = accountBalanceForAsset;
                    }
                    else
                    {
                        decimal balance = _random.NextDecimal(0.001M, 0.02M).PrecisionTruncate(3);
                        balancesForAsset = new AccountBalances()
                        {
                            Asset = uppercaseAsset,
                            Available = balance,
                            Locked = 0.0M,
                            Total = balance
                        };
                        _balancesByAssets[uppercaseAsset] = balancesForAsset;
                    }

                    accountBalances.Add(balancesForAsset);
                }
            });

            return accountBalances;
        }

        public async Task<decimal?> GetSpotPriceAsync(string symbol, string quoteAsset)
        {
            decimal price = await Task.Run(() => { return _random.NextDecimal(55000.00M, 60000.00M).PrecisionTruncate(2); });
            
            return price;
        }

        public bool Buy(string symbol, decimal quantity, decimal price)
        {
            decimal totalPrice = quantity * price;
            AccountBalances usd = _balancesByAssets["USDT"];

            if (usd.Available >= totalPrice && _balancesByAssets.TryGetValue(symbol.ToUpper(), out AccountBalances? asset))
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
            AccountBalances usd = _balancesByAssets["USDT"];

            if (_balancesByAssets.TryGetValue(symbol.ToUpper(), out AccountBalances? asset) && asset.Available >= quantity)
            {
                usd.Available += totalPrice;
                asset.Available -= quantity;

                return true;
            }

            return false;
        }
    }
}
