using CaptaTrader.Business.Modules.Trading.DTOs;
using CaptaTrader.Data;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices.CFGI;

// API Documentation: https://alternative.me/crypto/fear-and-greed-index/#api
public class AlternativeCFGI : ICryptoFearAndGreedIndex
{
    private const string ENDPOINT = "https://api.alternative.me/"; //TODO: Don't hard-code URLs; should be stored in configuration.
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
                JsonSerializer.Deserialize<AlternativeCFGIResultDto>(json) is AlternativeCFGIResultDto result)
            {
                return result.Data.Where(d => !string.IsNullOrEmpty(d.Value) && d.Value.All(char.IsDigit))
                    .Select(d => Convert.ToInt32(d.Value));
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
