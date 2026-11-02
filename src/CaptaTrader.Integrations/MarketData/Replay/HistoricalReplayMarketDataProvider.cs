using CaptaTrader.Business.Modules.MarketData;
using CaptaTrader.Business.Modules.Providers;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CaptaTrader.Integrations.MarketData.Replay;

/// <summary>
/// Replays a real, unusually volatile day of BTC-USD one-minute candles at accelerated speed.
/// </summary>
/// <remarks>
/// The bundled day is 2024-08-05 (UTC), the global risk-off selloff when BTC fell from about 58,000 to
/// about 49,000 USD and rebounded within hours. Candles were downloaded from the Coinbase Exchange public
/// candles endpoint (<see href="https://docs.cdp.coinbase.com/exchange/reference/exchangerestapi_getproductcandles"/>).
/// Each candle becomes four ticks, open then low/high then close, ordered the way the candle most
/// plausibly traded, so intra-minute swings are visible to the strategy.
/// </remarks>
internal sealed class HistoricalReplayMarketDataProvider : IMarketDataProvider
{
    internal const string PROVIDER_ID = "replay-2024-08-05";
    private const string RESOURCE_SUFFIX = "btc-usd-2024-08-05-1m.csv";
    private static readonly TimeSpan TICK_SPACING = TimeSpan.FromSeconds(15);

    private readonly Lazy<IReadOnlyList<Candle>> _candles = new(LoadCandles);

    public ProviderDescriptor Descriptor { get; } = new(
        PROVIDER_ID,
        ProviderKind.MarketData,
        "Historical replay: Aug 5 2024 crash and rebound",
        "Replays a real volatile day of BTC-USD minute candles at high speed. Simulated; only drives simulated brokerages.",
        IsSimulated: true,
        CredentialFields: []);

    /// <summary>Wall-clock delay between replayed ticks.</summary>
    internal TimeSpan TickInterval { get; init; } = TimeSpan.FromMilliseconds(25);

    internal IReadOnlyList<Candle> Candles => _candles.Value;

    public IMarketDataFeed Connect(ProviderConnection connection)
    {
        return new ReplayFeed(this);
    }

    internal static IEnumerable<MarketTick> ToTicks(Candle candle)
    {
        bool rising = candle.Close >= candle.Open;
        decimal first = rising ? candle.Low : candle.High;
        decimal second = rising ? candle.High : candle.Low;

        yield return new MarketTick(candle.Time, candle.Open);
        yield return new MarketTick(candle.Time + TICK_SPACING, first);
        yield return new MarketTick(candle.Time + (TICK_SPACING * 2), second);
        yield return new MarketTick(candle.Time + (TICK_SPACING * 3), candle.Close);
    }

    private static IReadOnlyList<Candle> LoadCandles()
    {
        Assembly assembly = typeof(HistoricalReplayMarketDataProvider).Assembly;
        string resourceName = assembly.GetManifestResourceNames().Single(name => name.EndsWith(RESOURCE_SUFFIX, StringComparison.Ordinal));

        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using StreamReader reader = new(stream);
        List<Candle> candles = [];

        // Header: TimeUtc,Open,High,Low,Close,Volume
        reader.ReadLine();

        while (reader.ReadLine() is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            string[] fields = line.Split(',');
            candles.Add(new Candle(
                DateTimeOffset.Parse(fields[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
                decimal.Parse(fields[1], CultureInfo.InvariantCulture),
                decimal.Parse(fields[2], CultureInfo.InvariantCulture),
                decimal.Parse(fields[3], CultureInfo.InvariantCulture),
                decimal.Parse(fields[4], CultureInfo.InvariantCulture)));
        }

        return candles;
    }

    internal sealed record Candle(DateTimeOffset Time, decimal Open, decimal High, decimal Low, decimal Close);

    private sealed class ReplayFeed : IMarketDataFeed
    {
        private readonly HistoricalReplayMarketDataProvider _provider;

        internal ReplayFeed(HistoricalReplayMarketDataProvider provider)
        {
            _provider = provider;
        }

        public async IAsyncEnumerable<MarketTick> StreamAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (Candle candle in _provider.Candles)
            {
                foreach (MarketTick tick in ToTicks(candle))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return tick;

                    if (_provider.TickInterval > TimeSpan.Zero)
                    {
                        await Task.Delay(_provider.TickInterval, cancellationToken);
                    }
                }
            }
        }
    }
}
