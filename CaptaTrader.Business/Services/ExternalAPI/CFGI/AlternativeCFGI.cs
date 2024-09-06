using CaptaTrader.Business.Models;
using CaptaTrader.Data;
using CryptoExchange.Net.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CaptaTrader.Business.Services
{
    // API Documentation: https://alternative.me/crypto/fear-and-greed-index/#api
    public class AlternativeCFGI : ICryptoFearAndGreedIndex
    {
        private const string ENDPOINT = "https://api.alternative.me/";
        private const string QUERY_FORMAT = "fng/?limit={0}&format=json&date_format=us";

        private readonly ILogger _logger;
        private readonly WebRequester _webRequester;

        public AlternativeCFGI(ILogger logger, WebRequester webRequester)
        {
            _logger = logger;
            _webRequester = webRequester;
        }

        public async Task<IEnumerable<int>?> GetCryptoFearAndGreedIndexAsync(int limit)
        {
            try
            {
                string url = $"{ENDPOINT}{string.Format(QUERY_FORMAT, limit)}";

                string? json = await _webRequester.SendHTTPJsonRequestAsync(url);

                if (!string.IsNullOrEmpty(json) && 
                    JsonSerializer.Deserialize<AlternativeCFGIResult>(json) is AlternativeCFGIResult result)
                {
                    return result.Data.Select(d => d.Value);
                }
                else
                {
                    _logger.LogError("Failed to retrieve Fear and Greed Index.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request Fear and Greed Index.");
            }

            return null;
        }
    }
}
