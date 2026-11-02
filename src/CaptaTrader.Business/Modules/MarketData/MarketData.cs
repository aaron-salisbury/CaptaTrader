using CaptaTrader.Business.Modules.Providers;
using System;
using System.Collections.Generic;
using System.Threading;

namespace CaptaTrader.Business.Modules.MarketData;

/// <summary>
/// One observed BTC-USD price. <see cref="Time"/> is the market time of the observation, which for
/// a historical replay is in the past; trading decisions and ledger timestamps use it rather than
/// the wall clock so replays produce coherent records.
/// </summary>
public sealed record MarketTick(DateTimeOffset Time, decimal Price, decimal? Bid = null, decimal? Ask = null);

/// <summary>
/// A Provider of BTC-USD market data.
/// </summary>
public interface IMarketDataProvider
{
    ProviderDescriptor Descriptor { get; }

    IMarketDataFeed Connect(ProviderConnection connection);
}

/// <summary>
/// A stream of price observations. Live feeds run until cancelled; replays end when history runs out.
/// Feeds retry transient failures themselves and simply pause the stream while unavailable.
/// </summary>
public interface IMarketDataFeed
{
    IAsyncEnumerable<MarketTick> StreamAsync(CancellationToken cancellationToken);
}
