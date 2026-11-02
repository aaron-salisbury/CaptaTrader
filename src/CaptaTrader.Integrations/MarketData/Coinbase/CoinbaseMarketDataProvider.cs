using CaptaTrader.Business.Modules.MarketData;
using CaptaTrader.Business.Modules.Providers;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CaptaTrader.Integrations.MarketData.Coinbase;

/// <summary>
/// Live BTC-USD prices from Coinbase Exchange's public ticker. No API key is required.
/// </summary>
/// <remarks>
/// API reference: <see href="https://docs.cdp.coinbase.com/exchange/reference/exchangerestapi_getproductticker"/>.
/// Public endpoints are rate limited per IP; polling every few seconds stays well inside the limit.
/// </remarks>
internal sealed class CoinbaseMarketDataProvider : IMarketDataProvider
{
    internal const string PROVIDER_ID = "coinbase-exchange";

    private static readonly TimeSpan MAXIMUM_BACKOFF = TimeSpan.FromMinutes(1);
    private static readonly JsonSerializerOptions JSON_OPTIONS = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CoinbaseMarketDataProvider> _logger;

    public CoinbaseMarketDataProvider(IHttpClientFactory httpClientFactory, TimeProvider timeProvider, ILogger<CoinbaseMarketDataProvider> logger)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ProviderDescriptor Descriptor { get; } = new(
        PROVIDER_ID,
        ProviderKind.MarketData,
        "Coinbase Exchange (live)",
        "Public BTC-USD ticker from Coinbase Exchange, polled every few seconds. No API key needed.",
        IsSimulated: false,
        CredentialFields: []);

    internal Uri TickerAddress { get; init; } = new("https://api.exchange.coinbase.com/products/BTC-USD/ticker");

    internal TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);

    public IMarketDataFeed Connect(ProviderConnection connection)
    {
        return new TickerFeed(this);
    }

    private sealed class TickerFeed : IMarketDataFeed
    {
        private readonly CoinbaseMarketDataProvider _provider;

        internal TickerFeed(CoinbaseMarketDataProvider provider)
        {
            _provider = provider;
        }

        public async IAsyncEnumerable<MarketTick> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            TimeSpan delay = _provider.PollInterval;

            while (!cancellationToken.IsCancellationRequested)
            {
                MarketTick? tick = null;

                try
                {
                    tick = await ReadTickerAsync(cancellationToken);
                    delay = _provider.PollInterval;
                }
                catch (Exception exception) when ((exception is HttpRequestException or JsonException or TaskCanceledException) && !cancellationToken.IsCancellationRequested)
                {
                    // Pause the stream with exponential backoff rather than ending the session on a blip.
                    delay = TimeSpan.FromTicks(Math.Min(MAXIMUM_BACKOFF.Ticks, delay.Ticks * 2));
                    _provider._logger.LogWarning(exception, "Coinbase ticker unavailable; retrying in {Delay}.", delay);
                }

                if (tick is not null)
                {
                    yield return tick;
                }

                await Task.Delay(delay, _provider._timeProvider, cancellationToken);
            }
        }

        private async Task<MarketTick> ReadTickerAsync(CancellationToken cancellationToken)
        {
            HttpClient httpClient = _provider._httpClientFactory.CreateClient();
            CoinbaseTicker ticker = await httpClient.GetFromJsonAsync<CoinbaseTicker>(_provider.TickerAddress, JSON_OPTIONS, cancellationToken)
                ?? throw new JsonException("Empty ticker response.");

            decimal price = Parse(ticker.Price) ?? throw new JsonException("Ticker has no price.");

            return new MarketTick(_provider._timeProvider.GetUtcNow(), price, Parse(ticker.Bid), Parse(ticker.Ask));
        }

        private static decimal? Parse(string? value)
        {
            return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal parsed) ? parsed : null;
        }
    }

    private sealed record CoinbaseTicker(
        [property: JsonPropertyName("price")] string? Price,
        [property: JsonPropertyName("bid")] string? Bid,
        [property: JsonPropertyName("ask")] string? Ask);
}
