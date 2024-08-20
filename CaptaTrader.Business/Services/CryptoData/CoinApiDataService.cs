// Ignore Spelling: Api

using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.DTOs;
using CaptaTrader.Business.Models;
using CaptaTrader.Data;
using Microsoft.Extensions.Logging;
using System.Security;
using System.Text.Json;

namespace CaptaTrader.Business.Services
{
    // API Documentation: https://docs.coinapi.io/
    internal class CoinApiDataService : ICryptoDataService
    {
        private const string ENDPOINT = "https://rest.coinapi.io/";
        private const string QUERY_FORMAT_OHLCV_LATEST = "v1/ohlcv/{0}_SPOT_{1}_USD/latest?period_id=1MTH&limit=1"; // {0} = Exchange ID, {1} = Asset ID Base         //TODO: Default limit is 100, maybe that could be useful?
        //private const  EXCHANGE_ID = "BINANCE"; //TODO: For some reason Binance returns 400.
        private const string EXCHANGE_ID = "BITSTAMP";

        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IKeyVaultService _keyVaultService;

        public CoinApiDataService(ILogger logger, IHttpClientFactory httpClientFactory, IKeyVaultService keyVaultService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _keyVaultService = keyVaultService;
        }

        public int GetMinimumSecondsBetweenCalls() => 1;

        public async Task<PriceInterval?> GetPricesOverInterval(string symbol, DateTime start, DateTime end)
        {
            PriceInterval? priceInterval = null;

            SecureString? securedAPIKey = _keyVaultService.GetValueForKey(nameof(AppData.CoinApiAPIKeyEncrypted));
            if (securedAPIKey == null)
            {
                _logger.LogError("No CoinAPI API key available.");
                return priceInterval;
            }

            try
            {
                // https://docs.coinapi.io/market-data/rest-api/metadata/list-all-symbols
                // https://docs.coinapi.io/market-data/rest-api/metadata/list-all-exchanges
                // Get all exchange IDs: https://rest.coinapi.io/v1/exchanges

                string url = $"{ENDPOINT}{string.Format(QUERY_FORMAT_OHLCV_LATEST, EXCHANGE_ID, symbol)}";

                string? jsonResponse = await WebRequests.SendHTTPJsonRequestAsync(
                    url,
                    _httpClientFactory,
                    _logger,
                    requestHeaders: new Dictionary<string, string>() { { "Authorization", securedAPIKey.ToPlainString() } },
                    namedClient: Manager.COMPRESSION_CLIENT_NAME); // CoinApi requests that compression be used.

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    CoinApiOhlcv? latest = JsonSerializer.Deserialize<List<CoinApiOhlcv>>(jsonResponse)?.FirstOrDefault();

                    if (latest != null)
                    {
                        priceInterval = new PriceInterval
                        {
                            Symbol = symbol,
                            StartDate = latest.TimePeriodStart,
                            EndDate = latest.TimePeriodEnd,
                            StartPrice = Convert.ToDecimal(latest.PriceOpen),
                            EndPrice = Convert.ToDecimal(latest.PriceClose)
                        };
                    }
                }
                else
                {
                    _logger.LogError("Failed to update historical price data response.");
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to update historical price data: {e.Message}");
            }

            return priceInterval;
        }
    }
}
