using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CaptaTrader.Services.Base
{
    public static class WebRequests
    {
        public static async Task<string> GetCurlResponseAsync(string curlURL, ILogger logger, Dictionary<string, string> requestHeaders = null)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    if (requestHeaders != null)
                    {
                        foreach (KeyValuePair<string, string> valueByName in requestHeaders)
                        {
                            client.DefaultRequestHeaders.TryAddWithoutValidation(valueByName.Key, valueByName.Value);
                        }
                    }

                    return await client.GetStringAsync(curlURL);
                }
            }
            catch (Exception e)
            {
                logger.Error($"Curl request failed: {e.Message}");
                return null;
            }
        }

        internal static async Task<Stream> GetWebRequestStreamAsync(string url, ILogger logger)
        {
            try
            {
                WebRequest request = WebRequest.Create(url);
                WebResponse response = await request.GetResponseAsync();

                return response.GetResponseStream();
            }
            catch (Exception e)
            {
                logger.Error($"Failed to complete web request: {e.Message}");
                return null;
            }

        }
    }
}
