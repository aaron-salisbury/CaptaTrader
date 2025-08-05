using CaptaTrader.Business.Base;
using CaptaTrader.Business.Modules.Trading.DTOs;
using Microsoft.Extensions.Logging;
using RunnethOverStudio.AppToolkit.Modules.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices;

// API Documentation: https://alternative.me/crypto/fear-and-greed-index/#api
internal sealed class AlternativeCFGI : ICryptoFearAndGreedIndex
{
    private const string QUERY_FORMAT = "fng/?limit={0}&format=json&date_format=us";

    private readonly ILogger _logger;
    private readonly IHttpRequester _httpRequester;

    public AlternativeCFGI(ILogger logger, IHttpRequester httpRequester)
    {
        _logger = logger;
        _httpRequester = httpRequester;
    }

    public async Task<IEnumerable<int>?> GetCryptoFearAndGreedIndexAsync(int limit)
    {
        try
        {
            string url = $"{Resources.AlternativeCFGIEndpoint}{string.Format(QUERY_FORMAT, limit)}";

            string? json = await _httpRequester.SendHTTPJsonRequestAsync(url);

            if (!string.IsNullOrEmpty(json) &&
                JsonSerializer.Deserialize<AlternativeCFGIResult>(json) is AlternativeCFGIResult result)
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
