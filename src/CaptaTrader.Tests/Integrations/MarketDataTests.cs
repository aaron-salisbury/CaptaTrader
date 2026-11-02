using CaptaTrader.Business.Modules.MarketData;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Integrations.MarketData.Coinbase;
using CaptaTrader.Integrations.MarketData.Replay;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Tests.Integrations;

[TestClass]
public sealed class MarketDataTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void Replay_BundlesAFullVolatileDay()
    {
        HistoricalReplayMarketDataProvider provider = new();

        Assert.HasCount(1_440, provider.Candles);
        Assert.AreEqual(new DateTimeOffset(2024, 8, 5, 0, 0, 0, TimeSpan.Zero), provider.Candles[0].Time);
        Assert.IsLessThan(50_000m, provider.Candles.Min(candle => candle.Low));
        Assert.IsGreaterThan(58_000m, provider.Candles.Max(candle => candle.High));
        Assert.IsTrue(provider.Descriptor.IsSimulated);
    }

    [TestMethod]
    public void Replay_OrdersIntraMinuteTicksByCandleDirection()
    {
        DateTimeOffset time = new(2024, 8, 5, 0, 0, 0, TimeSpan.Zero);

        decimal[] rising = [.. HistoricalReplayMarketDataProvider.ToTicks(new(time, 100m, 110m, 90m, 105m)).Select(tick => tick.Price)];
        decimal[] falling = [.. HistoricalReplayMarketDataProvider.ToTicks(new(time, 100m, 110m, 90m, 95m)).Select(tick => tick.Price)];

        Assert.AreSequenceEqual(new[] { 100m, 90m, 110m, 105m }, rising);
        Assert.AreSequenceEqual(new[] { 100m, 110m, 90m, 95m }, falling);
    }

    [TestMethod]
    public async Task Replay_StreamsInTimeOrder()
    {
        HistoricalReplayMarketDataProvider provider = new() { TickInterval = TimeSpan.Zero };
        DateTimeOffset previous = DateTimeOffset.MinValue;
        int count = 0;

        await foreach (MarketTick tick in provider.Connect(ProviderConnection.Anonymous).StreamAsync(TestContext.CancellationToken))
        {
            Assert.IsGreaterThan(previous, tick.Time);
            previous = tick.Time;
            count++;
        }

        Assert.AreEqual(1_440 * 4, count);
    }

    [TestMethod]
    public async Task Coinbase_ParsesTicker_AndRetriesThroughFailures()
    {
        int calls = 0;
        FakeHttp http = new((_, _) =>
        {
            calls++;
            return calls == 1
                ? FakeHttp.Json(HttpStatusCode.ServiceUnavailable, "{}")
                : FakeHttp.Json(HttpStatusCode.OK, """{"trade_id":1,"price":"65000.12","size":"0.1","bid":"65000.00","ask":"65000.50","volume":"1","time":"2026-06-01T12:00:00Z"}""");
        });

        CoinbaseMarketDataProvider provider = new(http, TimeProvider.System, NullLogger<CoinbaseMarketDataProvider>.Instance)
        {
            PollInterval = TimeSpan.FromMilliseconds(1)
        };

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        IAsyncEnumerator<MarketTick> ticks = provider.Connect(ProviderConnection.Anonymous).StreamAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);

        Assert.IsTrue(await ticks.MoveNextAsync());
        Assert.AreEqual(65000.12m, ticks.Current.Price);
        Assert.AreEqual(65000.00m, ticks.Current.Bid);
        Assert.AreEqual(65000.50m, ticks.Current.Ask);
        Assert.AreEqual(2, calls);
        Assert.IsFalse(provider.Descriptor.IsSimulated);

        await ticks.DisposeAsync();
    }
}
