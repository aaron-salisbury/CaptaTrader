using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using Binance.Net.Objects.Options;
using CaptaTrader.Business.Models;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Logging;

namespace CaptaTrader.Business.Services
{
    // API Documentation: https://docs.binance.us/#introduction
    // Helper Library: https://github.com/JKorf/Binance.Net
    public class BinanceExchangeService : IExchangeService
    {
        private readonly ILogger _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISessionValueResolver _sessionValueResolver;

        public BinanceExchangeService(ILogger logger, ILoggerFactory loggerFactory, IHttpClientFactory httpClientFactory, ISessionValueResolver sessionValueResolver)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _httpClientFactory = httpClientFactory;
            _sessionValueResolver = sessionValueResolver;
        }

        public string GetExchangeID() => "BINANCE";

        public async Task<IEnumerable<AssetBalance>?> GetAccountBalancesAsync(IEnumerable<string> assets)
        {
            try
            {
                using (HttpClient httpClient = _httpClientFactory.CreateClient())
                using (BinanceRestClient restClient = CreateDefaultRestClient(httpClient))
                {
                    WebCallResult<BinanceAccountInfo> result = await restClient.SpotApi.Account.GetAccountInfoAsync();

                    if (result.Error != null)
                    {
                        _logger.LogError("Failed to get account balance: {resultErrorMessage}", result.Error.Message);
                        return null;
                    }

                    return result.Data.Balances
                        .Where(b => assets.Contains(b.Asset))
                        .Select(b => new AssetBalance()
                        {
                            Asset = b.Asset,
                            Available = b.Available,
                            Locked = b.Locked,
                            Total = b.Total
                        });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get account balance.");
                return null;
            }
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

                    return price;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get spot price of {symbol}.", symbol);
                return null;
            }
        }

        public bool Buy(TradeOrder tradeOrder)
        {
            //try
            //{
            //    using (var client = new BinanceClient())
            //    {
            //        WebCallResult<BinancePlacedOrder> result = client.Spot.Order.PlaceOrder(symbol, OrderSide.Buy, OrderType.Limit, quantity, price: price, timeInForce: TimeInForce.ImmediateOrCancel);

            //        return result.Success;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Failed to buy {quantity} of {symbol} at ${price}.", quantity, symbol, price);
            //    return false;
            //}

            return false;
        }

        public bool Sell(TradeOrder tradeOrder)
        {
            //try
            //{
            //    using (var client = new BinanceClient())
            //    {
            //        WebCallResult<BinancePlacedOrder> result = client.Spot.Order.PlaceOrder(symbol, OrderSide.Sell, OrderType.Limit, quantity, price: price, timeInForce: TimeInForce.ImmediateOrCancel);

            //        return result.Success;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Failed to sell {quantity} of {symbol} at ${price}.", quantity, symbol, price);
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

            ApiCredentials? apiCredentials = GetApiCredentialsForSessionUser();
            if (apiCredentials != null)
            {
                restClient.SetApiCredentials(apiCredentials);
            }
            else
            {
                _logger.LogError("No API credentials available to exchange service.");
            }

            return restClient;
        }

        private ApiCredentials? GetApiCredentialsForSessionUser()
        {
            EndUser? sessionUser = _sessionValueResolver.GetSessionUser();
            if (sessionUser?.ExchangeAPIKey != null && sessionUser?.ExchangeSecretKey != null)
            {
                return new(sessionUser.ExchangeAPIKey, sessionUser.ExchangeSecretKey);
            }

            return null;
        }
    }
}
