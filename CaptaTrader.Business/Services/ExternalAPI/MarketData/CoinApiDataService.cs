using CaptaTrader.Data;
using CaptaTrader.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CaptaTrader.Business.Services
{
    // API Documentation: https://docs.coinapi.io/
    public class CoinApiDataService : IMarketDataService
    {
        private const string ENDPOINT = "https://rest.coinapi.io/"; //TODO: Don't hard-code URLs; should be stored in configuration.
        private const string QUERY_FORMAT_OHLCV_LATEST = "v1/ohlcv/{0}_SPOT_{1}_{2}/latest?period_id=1DAY&limit=30"; // {0} = Exchange ID, {1} = Base Asset, {2} = Quote Asset
        private const string QUOTE_ASSET = "USDT";

        private readonly ILogger _logger;
        private readonly WebRequester _webRequester;
        private readonly ISessionValueResolver _sessionValueResolver;

        public CoinApiDataService(ILogger logger, WebRequester webRequester, ISessionValueResolver sessionValueResolver)
        {
            _logger = logger;
            _webRequester = webRequester;
            _sessionValueResolver = sessionValueResolver;
        }

        public async Task<IEnumerable<PriceInterval>?> GetLastThirtyDaysOfPriceIntervals(string exchangeID, string baseAsset)
        {
            string? apiKey = _sessionValueResolver.GetSessionUser()?.MarketDataAPIKey;
            if (apiKey == null)
            {
                _logger.LogError("No API key available to market data service.");
                return null;
            }

            try
            {
                string url = $"{ENDPOINT}{string.Format(QUERY_FORMAT_OHLCV_LATEST, exchangeID, baseAsset, QUOTE_ASSET)}";

                string? json = await _webRequester.SendHTTPJsonRequestAsync(
                    url,
                    useCompression: true, // CoinApi requests that compression be used.
                    requestHeaders: new Dictionary<string, string>() { { "Authorization", apiKey } });

                if (!string.IsNullOrEmpty(json) &&
                    JsonSerializer.Deserialize<List<CoinApiOhlcv>>(json) is List<CoinApiOhlcv> intervals)
                {
                    return intervals.Select(cao => new PriceInterval()
                    {
                        Symbol = baseAsset,
                        StartDate = cao.TimePeriodStart,
                        EndDate = cao.TimePeriodEnd,
                        StartPrice = Convert.ToDecimal(cao.PriceOpen),
                        EndPrice = Convert.ToDecimal(cao.PriceClose)
                    }).OrderByDescending(ph => ph.StartDate);
                }
                else
                {
                    _logger.LogError("Failed to retrieve historical price data.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request historical price data.");
            }

            return null;
        }
    }
}
