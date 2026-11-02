using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.MarketData;
using CaptaTrader.Business.Modules.Strategies;
using System;

namespace CaptaTrader.Business.Modules.AutoTrading;

/// <summary>
/// Presentation-neutral notifications from a trading session. Hosts render these; the ledger is the
/// durable record, so a host that misses events loses nothing.
/// </summary>
public abstract record TradingEvent(DateTimeOffset At);

public sealed record SessionStarted(DateTimeOffset At, string BrokerageName, string MarketDataName, string StrategyName, bool IsSimulated) : TradingEvent(At);

public sealed record SessionStopped(DateTimeOffset At, string Reason) : TradingEvent(At);

/// <summary>An order was recorded on the ledger and is being sent to the brokerage.</summary>
public sealed record OrderAttempted(DateTimeOffset At, OrderAttempt Attempt) : TradingEvent(At);

/// <summary>An order reached a final (or unknown) state. <paramref name="RealizedGain"/> is set for filled sells.</summary>
public sealed record OrderCompleted(DateTimeOffset At, OrderAttempt Attempt, decimal? RealizedGain) : TradingEvent(At);

/// <summary>A strategy proposal failed a pre-trade check and was not sent.</summary>
public sealed record OrderBlocked(DateTimeOffset At, TradeDecision Decision, string Reason) : TradingEvent(At);

public enum NoticeLevel
{
    Information,
    Warning,
    Error
}

public sealed record TradingNotice(DateTimeOffset At, NoticeLevel Level, string Message) : TradingEvent(At);

/// <summary>
/// Point-in-time view of the trading session for status displays.
/// </summary>
public sealed record TradingStatus(
    bool IsRunning,
    string? BrokerageName,
    string? MarketDataName,
    string? StrategyName,
    bool IsSimulated,
    MarketTick? LastTick,
    decimal? SessionOpenPrice,
    AccountSnapshot? Account,
    decimal HeldQuantity,
    decimal HeldCostBasis,
    int OrdersInLastHour,
    int MaxOrdersPerHour,
    OrderAttempt? InFlight,
    int Attempts,
    int Fills,
    decimal RealizedGain,
    string? LastDecision)
{
    public static TradingStatus Idle { get; } = new(false, null, null, null, false, null, null, null, 0m, 0m, 0, 0, null, 0, 0, 0m, null);

    /// <summary>Unrealized gain of the app-held position at the last price, before sell fees and tax.</summary>
    public decimal? UnrealizedGain => LastTick is null ? null : (HeldQuantity * LastTick.Price) - HeldCostBasis;
}
