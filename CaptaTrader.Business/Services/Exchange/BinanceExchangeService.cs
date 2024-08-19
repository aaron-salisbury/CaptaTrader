// Ignore Spelling: Binance

using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using Binance.Net.Objects.Options;
using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.DTOs;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Logging;
using System.Security;

namespace CaptaTrader.Business.Services
{
    // API Documentation: https://docs.binance.us/#introduction
    // Helper Library: https://github.com/JKorf/Binance.Net
    internal class BinanceExchangeService : IExchangeService
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApiCredentials _apiCredentials;

        public BinanceExchangeService(ILogger logger, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, IKeyVaultService keyVaultService)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _httpClientFactory = httpClientFactory;
            _apiCredentials = SetApiCredentials(keyVaultService);
        }

        public decimal? GetAccountBalance()
        {
            //try
            //{
            //    using (BinanceClient client = new())
            //    {
            //        WebCallResult<BinanceAccountInfo> result = client.General.GetAccountInfo();

            //        if (result.Error != null)
            //        {
            //            _logger.LogError($"Failed to get account balance: {result.Error.Message}");
            //            return null;
            //        }

            //        return result.Data.Balances.Where(b => string.Equals(b.Asset, "USD")).FirstOrDefault()?.Free;
            //    }
            //}
            //catch (Exception e)
            //{
            //    _logger.LogError($"Failed to get account balance: {e.Message}");
            //    return null;
            //}

            return 0.0M;
        }

        public decimal? GetAssetBalance(string symbol)
        {
            //try
            //{
            //    using (BinanceClient client = new())
            //    {
            //        WebCallResult<BinanceAccountInfo> result = client.General.GetAccountInfo();

            //        if (result.Error != null)
            //        {
            //            _logger.LogError($"Failed to get asset balance of {symbol}: {result.Error.Message}");
            //            return null;
            //        }

            //        return result.Data.Balances.Where(b => string.Equals(b.Asset, symbol)).FirstOrDefault()?.Total;
            //    }
            //}
            //catch (Exception e)
            //{
            //    _logger.LogError($"Failed to get asset balance of {symbol}: {e.Message}");
            //    return null;
            //}

            return 0.0M;
        }

        public async Task<decimal?> GetSpotPriceAsync(string symbol, string quoteAsset)
        {
            try
            {
                using (HttpClient httpClient = _httpClientFactory.CreateClient())
                using (BinanceRestClient restClient = CreateDefaultRestClient(httpClient))
                {
                    WebCallResult<BinanceBookPrice> result = await restClient.SpotApi.ExchangeData.GetBookPriceAsync($"{symbol}{quoteAsset}");
                    decimal price = result.Data.BestAskPrice;

                    //var tickerResult = await restClient.SpotApi.ExchangeData.GetTickerAsync("ETHUSDT");
                    //var lastPrice = tickerResult.Data.LastPrice;

                    return price;
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
            //try
            //{
            //    using (var client = new BinanceClient())
            //    {
            //        WebCallResult<BinancePlacedOrder> result = client.Spot.Order.PlaceOrder(symbol, OrderSide.Buy, OrderType.Limit, quantity, price: price, timeInForce: TimeInForce.ImmediateOrCancel);

            //        return result.Success;
            //    }
            //}
            //catch (Exception e)
            //{
            //    _logger.LogError($"Failed to buy {quantity} of {symbol} at ${price}: {e.Message}");
            //    return false;
            //}

            return false;
        }

        public bool Sell(string symbol, decimal quantity, decimal price)
        {
            //try
            //{
            //    using (var client = new BinanceClient())
            //    {
            //        WebCallResult<BinancePlacedOrder> result = client.Spot.Order.PlaceOrder(symbol, OrderSide.Sell, OrderType.Limit, quantity, price: price, timeInForce: TimeInForce.ImmediateOrCancel);

            //        return result.Success;
            //    }
            //}
            //catch (Exception e)
            //{
            //    _logger.LogError($"Failed to sell {quantity} of {symbol} at ${price}: {e.Message}");
            //    return false;
            //}

            return false;
        }

        private BinanceRestClient CreateDefaultRestClient(HttpClient httpClient)
        {
            BinanceRestClient restClient = new(
                httpClient,
                _loggerFactory,
                (BinanceRestOptions options) => options.Environment = BinanceEnvironment.Us); // https://dev.binance.vision/t/error-restricted-location-according-b-eligibility/13838/2

            restClient.SetApiCredentials(_apiCredentials);

            return restClient;
        }

        private ApiCredentials SetApiCredentials(IKeyVaultService keyVaultService)
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
                    return new(securedAPIKey.ToPlainString(), securedSecretAPIKey.ToPlainString());
                }
            }

            //TODO: Handle this properly.
            throw new Exception("Binance credentials are required.");
        }
    }
}
