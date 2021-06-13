using CaptaTrader.Domains;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Xml;

namespace CaptaTrader.Services
{
    // API Documentation: https://nomics.com/docs/#section/Introduction
    public class NomicsCryptoDataService : ICryptoDataService
    {
        private const string API_SERVER_URL = "https://api.nomics.com/v1";
        private const string SPARKLINE_QUERY_PREFIX = "/currencies/sparkline";

        private ILogger _logger;
        private string _apiKey;

        public NomicsCryptoDataService(ILogger logger, string apiKey)
        {
            _logger = logger;
            _apiKey = apiKey;

            //TODO: Create a http client to reuse with web requests.
        }

        public async Task<PriceInterval> GetPricesOverInterval(string symbol, DateTime start, DateTime end)
        {
            string keyParam = $"?key={_apiKey}";
            string idParam = $"&ids={symbol}";

            string startRFC3339Escaped = Uri.EscapeUriString(XmlConvert.ToString(start, XmlDateTimeSerializationMode.Utc));
            string endRFC3339Escaped = Uri.EscapeUriString(XmlConvert.ToString(end, XmlDateTimeSerializationMode.Utc));

            string startParam = $"&start={startRFC3339Escaped}";
            string endParam = $"&end={endRFC3339Escaped}";

            string curl = $"{API_SERVER_URL}{SPARKLINE_QUERY_PREFIX}{keyParam}{idParam}{startParam}{endParam}";

            string response = await WebRequests.GetCurlResponseAsync(curl, _logger);
            //TODO: to convert dates back: XmlConvert.ToDateTime(string s, XmlDateTimeSerializationMode dateTimeOption)
            
            throw new NotImplementedException();
        }
    }
}
