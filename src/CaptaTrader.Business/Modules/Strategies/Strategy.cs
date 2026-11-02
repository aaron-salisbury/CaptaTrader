using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.MarketData;
using CaptaTrader.Business.Modules.Settings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaptaTrader.Business.Modules.Strategies;

/// <summary>
/// App-facing description of a strategy, so hosts can offer a choice without knowing types.
/// </summary>
public sealed record StrategyDescriptor(string Id, string DisplayName, string Description);

/// <summary>
/// Everything a strategy may consider for one decision. Strategies are pure decision functions over
/// this context: they never place orders, so every order passes the same pre-trade checks.
/// </summary>
/// <param name="Tick">The newest observation.</param>
/// <param name="RecentTicks">Recent observations, oldest first, including <paramref name="Tick"/>.</param>
/// <param name="Account">Brokerage-reported balances.</param>
/// <param name="OpenLots">Lots the app bought and still holds, oldest first. Strategies may only sell these.</param>
/// <param name="RemainingOrderBudget">Orders still allowed in the current rolling hour, so a strategy can weigh
/// a small profit now against a larger one later when attempts are scarce.</param>
public sealed record StrategyContext(
    MarketTick Tick,
    IReadOnlyList<MarketTick> RecentTicks,
    AccountSnapshot Account,
    IReadOnlyList<TaxLot> OpenLots,
    BrokerageRules Rules,
    TradingSettings Settings,
    int RemainingOrderBudget)
{
    public decimal HeldQuantity => OpenLots.Sum(lot => lot.RemainingQuantity);

    /// <summary>Price a market sell would likely receive.</summary>
    public decimal SellPrice => Tick.Bid ?? Tick.Price;

    /// <summary>Price a market buy would likely pay.</summary>
    public decimal BuyPrice => Tick.Ask ?? Tick.Price;
}

public enum TradeAction
{
    Hold,
    Buy,
    Sell
}

/// <summary>
/// A strategy's proposal. Buys carry a USD <see cref="Notional"/>; sells carry a BTC <see cref="Quantity"/>.
/// </summary>
public sealed record TradeDecision(TradeAction Action, decimal Notional, decimal Quantity, string Reason)
{
    public static TradeDecision Hold(string reason)
    {
        return new TradeDecision(TradeAction.Hold, 0m, 0m, reason);
    }

    public static TradeDecision Buy(decimal notional, string reason)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(notional);

        return new TradeDecision(TradeAction.Buy, notional, 0m, reason);
    }

    public static TradeDecision Sell(decimal quantity, string reason)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        return new TradeDecision(TradeAction.Sell, 0m, quantity, reason);
    }
}

/// <summary>
/// A trading strategy. Implementations live in Business because deciding when to trade is a
/// business rule; they depend only on the context, never on a specific Provider.
/// </summary>
public interface ITradingStrategy
{
    StrategyDescriptor Descriptor { get; }

    /// <summary>Called once at session start so stateful strategies can reset.</summary>
    void Reset();

    TradeDecision Evaluate(StrategyContext context);
}

/// <summary>
/// Strategies available in this build.
/// </summary>
public sealed class StrategyCatalog
{
    private readonly IReadOnlyList<ITradingStrategy> _strategies;

    public StrategyCatalog(IEnumerable<ITradingStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);

        _strategies = [.. strategies];
    }

    public IReadOnlyList<StrategyDescriptor> Descriptors => [.. _strategies.Select(strategy => strategy.Descriptor)];

    public StrategyDescriptor? FindDescriptor(string? strategyId)
    {
        return Find(strategyId)?.Descriptor;
    }

    internal ITradingStrategy? Find(string? strategyId)
    {
        return _strategies.FirstOrDefault(strategy => string.Equals(strategy.Descriptor.Id, strategyId, StringComparison.OrdinalIgnoreCase));
    }
}
