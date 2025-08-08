using CaptaTrader.Business.Base;
using CaptaTrader.Business.Modules.Trading.DTOs;
using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using Microsoft.Extensions.Logging;
using RunnethOverStudio.AppToolkit.Modules.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices;

// API Documentation: https://docs.coinapi.io/
public sealed class CoinApiDataService : IMarketDataService
{
    private const string QUERY_FORMAT_OHLCV_LATEST = "v1/ohlcv/{0}_SPOT_{1}_{2}/latest?period_id=1DAY&limit=30"; // {0} = Exchange ID, {1} = Base Asset, {2} = Quote Asset
    private const string QUOTE_ASSET = "USDT";

    private readonly ILogger _logger;
    private readonly IHttpRequester _httpRequester;
    private readonly IUserAccessService _userAccessService;

    public CoinApiDataService(ILogger logger, IHttpRequester httpRequester, IUserAccessService userAccessService)
    {
        _logger = logger;
        _httpRequester = httpRequester;
        _userAccessService = userAccessService;
    }

    public async Task<IEnumerable<PriceInterval>?> GetLastThirtyDaysOfPriceIntervals(string exchangeID, string baseAsset)
    {
        string? apiKey = (await _userAccessService.GetSessionUserConfigAsync())?.MarketDataAPIKey;

        if (apiKey == null)
        {
            _logger.LogError("No API key available for market data service.");
            return null;
        }

        try
        {
            string url = $"{Resources.CoinApiEndpoint}{string.Format(QUERY_FORMAT_OHLCV_LATEST, exchangeID, baseAsset, QUOTE_ASSET)}";

            string? json = await _httpRequester.SendHTTPJsonRequestAsync(
                url,
                new HTTPJsonRequest()
                {
                    UseCompression = true, // CoinApi requests that compression be used.
                    RequestHeaders = new Dictionary<string, string>() { { "Authorization", apiKey } }
                });

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
