// Ignore Spelling: Api

using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services.DTOs;
using CaptaTrader.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CaptaTrader.Business.Services
{
    // API Documentation: https://docs.coinapi.io/
    internal class CoinApiDataService : ICryptoDataService
    {
        private const string ENDPOINT = "https://rest.coinapi.io/";
        private const string QUERY_FORMAT_OHLCV_LATEST = "v1/ohlcv/{0}_SPOT_{1}_{2}/latest?period_id=1DAY&limit=30"; // {0} = Exchange ID, {1} = Base Asset, {2} = Quote Asset         //TODO: Default limit is 100, maybe that could be useful?
        private const string EXCHANGE_ID = "BINANCE";
        private const string BASE_ASSET = "BTC";
        private const string QUOTE_ASSET = "USDT";

        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Func<string?> _getKeyFunction;

        public CoinApiDataService(ILogger logger, IHttpClientFactory httpClientFactory, Func<string?> getKeyFunction)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _getKeyFunction = getKeyFunction;   
        }

        public async Task<IEnumerable<PriceInterval>?> GetLastThirtyDaysOfPriceIntervals()
        {
            List<PriceInterval> priceIntervals = [];

            string? key = _getKeyFunction();
            if (key == null)
            {
                _logger.LogError("CoinAPI key unavailable..");
                return null;
            }

            try
            {
                string url = $"{ENDPOINT}{string.Format(QUERY_FORMAT_OHLCV_LATEST, EXCHANGE_ID, BASE_ASSET, QUOTE_ASSET)}";

                string? jsonResponse = await WebRequests.SendHTTPJsonRequestAsync(
                    url,
                    _httpClientFactory,
                    _logger,
                    requestHeaders: new Dictionary<string, string>() { { "Authorization", key } },
                    namedClient: AppManager.COMPRESSION_CLIENT_NAME); // CoinApi requests that compression be used.

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
                        }).OrderByDescending(ph => ph.StartDate);
                    }
                }
                else
                {
                    _logger.LogError("Failed to retrieve historical price data.");
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
