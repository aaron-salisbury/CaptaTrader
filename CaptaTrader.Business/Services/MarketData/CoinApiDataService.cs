using CaptaTrader.Business.Base;
using CaptaTrader.Business.Models;
using CaptaTrader.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CaptaTrader.Business.Services
{
    // API Documentation: https://docs.coinapi.io/
    public class CoinApiDataService : IMarketDataService
    {
        private const string ENDPOINT = "https://rest.coinapi.io/";
        private const string QUERY_FORMAT_OHLCV_LATEST = "v1/ohlcv/{0}_SPOT_{1}_{2}/latest?period_id=1DAY&limit=30"; // {0} = Exchange ID, {1} = Base Asset, {2} = Quote Asset
        private const string QUOTE_ASSET = "USDT";

        private readonly ILogger _logger;
        private readonly WebRequester _webRequester;
        private readonly ISessionValueProvider _sessionValueProvider;

        public CoinApiDataService(ILogger logger, WebRequester webRequester, ISessionValueProvider sessionValueProvider)
        {
            _logger = logger;
            _webRequester = webRequester;
            _sessionValueProvider = sessionValueProvider;
        }

        public async Task<IEnumerable<PriceInterval>?> GetLastThirtyDaysOfPriceIntervals(string exchangeID, string baseAsset)
        {
            string? apiKey = _sessionValueProvider.GetUser()?.MarketDataAPIKey;
            if (apiKey == null)
            {
                _logger.LogError("No API key available to market data service.");
                return null;
            }

            try
            {
                string url = $"{ENDPOINT}{string.Format(QUERY_FORMAT_OHLCV_LATEST, exchangeID, baseAsset, QUOTE_ASSET)}";

                string? jsonResponse = await _webRequester.SendHTTPJsonRequestAsync(
                    url,
                    useCompression: true, // CoinApi requests that compression be used.
                    requestHeaders: new Dictionary<string, string>() { { "Authorization", apiKey } });

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    List<CoinApiOhlcv>? priceHistories = JsonSerializer.Deserialize<List<CoinApiOhlcv>>(jsonResponse);

                    if (priceHistories != null)
                    {
                        return priceHistories.Select(cao => new PriceInterval()
                        {
                            Symbol = baseAsset,
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
                _logger.LogError(ex, "Failed to update historical price data.");
            }

            return null;
        }
    }
}
