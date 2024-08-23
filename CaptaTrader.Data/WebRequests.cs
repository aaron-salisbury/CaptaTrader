// Ignore Spelling: Json

using CaptaTrader.Data.Base.Extensions;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;

namespace CaptaTrader.Data
{
    public static class WebRequests
    {
        public static async Task<string?> SendHTTPJsonRequestAsync(string url, IHttpClientFactory httpClientFactory, ILogger logger, string? jsonContent = null, Dictionary<string, string>? requestHeaders = null, string httpMethod = "GET", string? userAgent = null, string? namedClient = null)
        {
            try
            {
                using (HttpClient client = (namedClient == null ? httpClientFactory.CreateClient() : httpClientFactory.CreateClient(namedClient)))
                {
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    if (userAgent != null)
                    {
                        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                    }

                    if (requestHeaders != null)
                    {
                        foreach (KeyValuePair<string, string> valueByName in requestHeaders)
                        {
                            client.DefaultRequestHeaders.TryAddWithoutValidation(valueByName.Key, valueByName.Value);
                        }
                    }

                    if (string.IsNullOrEmpty(jsonContent) && string.Equals(httpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                    {
                        string response = await client.GetStringAsync(url);
                        return response;
                    }

                    using (HttpRequestMessage request = new(new HttpMethod(httpMethod), url))
                    {
                        if (!string.IsNullOrEmpty(jsonContent))
                        {
                            request.Content = new StringContent(jsonContent);
                            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");
                        }

                        HttpResponseMessage httpResponse = await client.SendAsync(request);
                        string response = await httpResponse.Content.ReadAsStringAsync();
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(default, ex, "HTTP request failed.");
                return null;
            }
        }

        public static async Task DownloadFileAsync(IHttpClientFactory httpClientFactory, string url, string fullFilePath, bool deletePreexisting = false)
        {
            if (deletePreexisting && File.Exists(fullFilePath))
            {
                File.Delete(fullFilePath);
            }

            using (HttpClient client = httpClientFactory.CreateClient())
            {
                await client.DownloadFileTaskAsync(new Uri(url), fullFilePath);
            }
        }

        public static async Task<Stream> GetWebRequestStreamAsync(string url, IHttpClientFactory httpClientFactory, ILogger logger)
        {
            HttpResponseMessage? response = await GetWebRequestResponseAsync(url, httpClientFactory, logger);

            if (response != null)
            {
                return await response.Content.ReadAsStreamAsync();

            }

            return new MemoryStream();
        }

        public static async Task<byte[]> GetWebRequestSerializedAsync(string url, IHttpClientFactory httpClientFactory, ILogger logger)
        {
            HttpResponseMessage? response = await GetWebRequestResponseAsync(url, httpClientFactory, logger);

            if (response != null)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }

            return [];

        }

        private static async Task<HttpResponseMessage?> GetWebRequestResponseAsync(string url, IHttpClientFactory httpClientFactory, ILogger logger)
        {
            try
            {
                using (HttpClient client = httpClientFactory.CreateClient())
                {
                    return await client.GetAsync(url);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(default, ex, "Web request failed.");
                return null;
            }
        }
    }
}
