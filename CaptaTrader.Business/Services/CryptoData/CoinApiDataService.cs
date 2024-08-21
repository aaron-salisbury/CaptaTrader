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
        private const string QUERY_FORMAT_OHLCV_LATEST = "v1/ohlcv/{0}_SPOT_{1}_{2}/latest?period_id=1MTH&limit=1"; // {0} = Exchange ID, {1} = Base Asset, {2} = Quote Asset         //TODO: Default limit is 100, maybe that could be useful?
        private const string EXCHANGE_ID = "BINANCE";
        private const string BASE_ASSET = "BTC";
        private const string QUOTE_ASSET = "USDT";

        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IKeyVaultService _keyVaultService;

        public CoinApiDataService(ILogger logger, IHttpClientFactory httpClientFactory, IKeyVaultService keyVaultService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _keyVaultService = keyVaultService;
        }

        public async Task<IEnumerable<PriceInterval>?> GetOneMonthPriceIntervals()
        {
            List<PriceInterval> priceIntervals = [];

            SecureString? securedAPIKey = _keyVaultService.GetValueForKey(nameof(AppData.CoinApiAPIKeyEncrypted));
            if (securedAPIKey == null)
            {
                _logger.LogError("No CoinAPI API key available.");
                return priceIntervals;
            }

            try
            {
                string url = $"{ENDPOINT}{string.Format(QUERY_FORMAT_OHLCV_LATEST, EXCHANGE_ID, BASE_ASSET, QUOTE_ASSET)}";

                string? jsonResponse = await WebRequests.SendHTTPJsonRequestAsync(
                    url,
                    _httpClientFactory,
                    _logger,
                    requestHeaders: new Dictionary<string, string>() { { "Authorization", securedAPIKey.ToPlainString() } },
                    namedClient: Manager.COMPRESSION_CLIENT_NAME); // CoinApi requests that compression be used.

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    List<CoinApiOhlcv>? priceHistories = JsonSerializer.Deserialize<List<CoinApiOhlcv>>(jsonResponse);

                    if (priceHistories != null)
                    {
                        return priceHistories.Select(cao => new PriceInterval()
                        {
                            Symbol = BASE_ASSET,
                            StartDate = cao.TimePeriodStart,
                            EndDate = cao.TimePeriodEnd,
                            StartPrice = Convert.ToDecimal(cao.PriceOpen),
                            EndPrice = Convert.ToDecimal(cao.PriceClose)
                        });
                    }
                }
                else
                {
                    _logger.LogError("Failed to update historical price data response.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(default, ex, "Failed to update historical price data.");
            }

            return null;
        }
    }
}
