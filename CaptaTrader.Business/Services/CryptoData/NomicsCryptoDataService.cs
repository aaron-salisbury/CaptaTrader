using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.DTOs;
using CaptaTrader.Business.Models;
using CaptaTrader.Data;
using Microsoft.Extensions.Logging;
using System.Security;
using System.Text.Json;
using System.Xml;

namespace CaptaTrader.Business.Services
{
    // **************** NOMICS NO LONGER EXISTS ****************

    // API Documentation: https://nomics.com/docs/#section/Introduction
    internal class NomicsCryptoDataService : ICryptoDataService
    {
        private const string API_SERVER_URL = "https://api.nomics.com/v1";
        private const string SPARKLINE_QUERY_PREFIX = "/currencies/sparkline";

        private readonly ILogger _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IKeyVaultService _keyVaultService;

        public NomicsCryptoDataService(ILogger logger, IHttpClientFactory httpClientFactory, IKeyVaultService keyVaultService)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _keyVaultService = keyVaultService;
        }

        public int GetMinimumSecondsBetweenCalls() => 1; // Nomics.com’s free plan allows for 1 request / second.

        public async Task<PriceInterval?> GetPricesOverInterval(string symbol, DateTime start, DateTime end)
        {
            PriceInterval? priceInterval = null;

            SecureString? securedAPIKey = _keyVaultService.GetValueForKey(nameof(AppData.NomicsAPIKeyEncrypted));
            if (securedAPIKey == null)
            {
                _logger.LogError("No Nomics API key available.");
                return priceInterval;
            }

            string keyParam = $"?key={securedAPIKey.ToPlainString()}";
            string idParam = $"&ids={symbol}";

            string startRFC3339Escaped = Uri.EscapeDataString(XmlConvert.ToString(start, XmlDateTimeSerializationMode.Utc));
            string endRFC3339Escaped = Uri.EscapeDataString(XmlConvert.ToString(end, XmlDateTimeSerializationMode.Utc));

            string startParam = $"&start={startRFC3339Escaped}";
            string endParam = $"&end={endRFC3339Escaped}";

            string curl = $"{API_SERVER_URL}{SPARKLINE_QUERY_PREFIX}{keyParam}{idParam}{startParam}{endParam}";

            try
            {
                string? jsonResponse = await WebRequests.SendHTTPJsonRequestAsync(curl, _httpClientFactory, _logger);

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    NomicsCurrenciesSparkline? currenciesSparkline = JsonSerializer.Deserialize<List<NomicsCurrenciesSparkline>>(jsonResponse)?.FirstOrDefault();

                    if (currenciesSparkline != null)
                    {
                        if (decimal.TryParse(currenciesSparkline.Prices.First(), out decimal startPrice) && decimal.TryParse(currenciesSparkline.Prices.Last(), out decimal endPrice))
                        {
                            priceInterval = new PriceInterval
                            {
                                Symbol = currenciesSparkline.Currency,
                                StartDate = currenciesSparkline.Timestamps.First(),
                                EndDate = currenciesSparkline.Timestamps.Last(),
                                StartPrice = startPrice,
                                EndPrice = endPrice
                            };
                        }
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
