// Ignore Spelling: Binance

using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services
{
    internal class MockExchangeService : IExchangeService
    {
        private readonly Random _random = new();
        private decimal usdBalance = 250000.00M;
        private decimal btcBalance = 0.025M;

        public async Task<IEnumerable<AccountBalances>?> GetAccountBalancesAsync(IEnumerable<string> assets)
        {
            List<AccountBalances> accountBalances = [];

            await Task.Run(() =>
            {
                accountBalances = new List<AccountBalances>()
                {
                    new AccountBalances()
                    {
                        Asset = "USD",
                        Available = usdBalance,
                        Locked = 0.0M,
                        Total = usdBalance
                    },
                    new AccountBalances()
                    {
                        Asset = "BTC",
                        Available = btcBalance,
                        Locked = 0.0M,
                        Total = btcBalance
                    }
                };
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

            if (usdBalance >= totalPrice)
            {
                usdBalance -= totalPrice;
                btcBalance += quantity;

                return true;
            }

            return false;
        }

        public bool Sell(string symbol, decimal quantity, decimal price)
        {
            decimal totalPrice = quantity * price;

            if (btcBalance >= quantity)
            {
                usdBalance += totalPrice;
                btcBalance -= quantity;

                return true;
            }

            return false;
        }
    }
}
