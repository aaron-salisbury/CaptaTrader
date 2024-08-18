using Binance.Net;
using Binance.Net.Enums;
using Binance.Net.Objects.Spot;
using Binance.Net.Objects.Spot.MarketData;
using Binance.Net.Objects.Spot.SpotData;
using CaptaTrader.Business.DTOs;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Logging;
using System.Security;

namespace CaptaTrader.Business.Services
{
    // API Examples: https://github.com/JKorf/Binance.Net/blob/master/Examples/ClientConsole/Program.cs
    //               https://github.com/JKorf/Binance.Net/blob/master/Examples/ClientWPF/ViewModels/MainViewModel.cs
    internal class BinanceExchangeService : IExchangeService
    {
        private const string ENDPOINT = "https://api.binance.us";

        private readonly ILogger _logger;

        public BinanceExchangeService(ILogger logger, IKeyVaultService keyVaultService)
        {
            _logger = logger;

            SetDefaultOptions(keyVaultService);
        }

        internal void SetDefaultOptions(IKeyVaultService keyVaultService)
        {
            SecureString? securedAPIKey = keyVaultService.GetValueForKey(nameof(AppData.BinanceAPIKeyEncrypted));
            if (securedAPIKey == null)
            {
                _logger.LogError("No Binance API key available.");
            }
            else
            {
                SecureString? securedSecretAPIKey = keyVaultService.GetValueForKey(nameof(AppData.BinanceSecretAPIKeyEncrypted));
                if (securedSecretAPIKey == null)
                {
                    _logger.LogError("No Binance Secret API key available.");
                }
                else
                {
                    BinanceClient.SetDefaultOptions(new BinanceClientOptions()
                    {
                        BaseAddress = ENDPOINT,
                        ApiCredentials = new ApiCredentials(securedAPIKey, securedSecretAPIKey),
                        LogVerbosity = LogVerbosity.Debug,
                        LogWriters = [Console.Out]
                    });

                    BinanceSocketClient.SetDefaultOptions(new BinanceSocketClientOptions()
                    {
                        BaseAddress = ENDPOINT,
                        ApiCredentials = new ApiCredentials(securedAPIKey, securedSecretAPIKey),
                        LogVerbosity = LogVerbosity.Debug,
                        LogWriters = [Console.Out]
                    });
                }
            }
        }

        public decimal? GetAccountBalance()
        {
            try
            {
                using (BinanceClient client = new())
                {
                    WebCallResult<BinanceAccountInfo> result = client.General.GetAccountInfo();

                    if (result.Error != null)
                    {
                        _logger.LogError($"Failed to get account balance: {result.Error.Message}");
                        return null;
                    }

                    return result.Data.Balances.Where(b => string.Equals(b.Asset, "USD")).FirstOrDefault()?.Free;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to get account balance: {e.Message}");
                return null;
            }
        }

        public decimal? GetAssetBalance(string symbol)
        {
            try
            {
                using (BinanceClient client = new())
                {
                    WebCallResult<BinanceAccountInfo> result = client.General.GetAccountInfo();

                    if (result.Error != null)
                    {
                        _logger.LogError($"Failed to get asset balance of {symbol}: {result.Error.Message}");
                        return null;
                    }

                    return result.Data.Balances.Where(b => string.Equals(b.Asset, symbol)).FirstOrDefault()?.Total;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to get asset balance of {symbol}: {e.Message}");
                return null;
            }
        }

        public decimal? GetSpotPrice(string symbol)
        {
            try
            {
                using (BinanceClient client = new())
                {
                    WebCallResult<BinanceBookPrice> result = client.Spot.Market.GetBookPrice(symbol);

                    return result.Data.BestAskPrice;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to get spot price of {symbol}: {e.Message}");
                return null;
            }
        }

        public bool Buy(string symbol, decimal quantity, decimal price)
        {
            try
            {
                using (var client = new BinanceClient())
                {
                    WebCallResult<BinancePlacedOrder> result = client.Spot.Order.PlaceOrder(symbol, OrderSide.Buy, OrderType.Limit, quantity, price: price, timeInForce: TimeInForce.ImmediateOrCancel);

                    return result.Success;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to buy {quantity} of {symbol} at ${price}: {e.Message}");
                return false;
            }
        }

        public bool Sell(string symbol, decimal quantity, decimal price)
        {
            try
            {
                using (var client = new BinanceClient())
                {
                    WebCallResult<BinancePlacedOrder> result = client.Spot.Order.PlaceOrder(symbol, OrderSide.Sell, OrderType.Limit, quantity, price: price, timeInForce: TimeInForce.ImmediateOrCancel);

                    return result.Success;
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to sell {quantity} of {symbol} at ${price}: {e.Message}");
                return false;
            }
        }
    }
}
