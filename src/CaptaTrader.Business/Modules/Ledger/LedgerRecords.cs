using CaptaTrader.Business.Modules.Brokerage;
using System;
using System.Collections.Generic;

namespace CaptaTrader.Business.Modules.Ledger;

public enum OrderAttemptStatus
{
    /// <summary>Recorded before submission; still awaiting the brokerage.</summary>
    Pending,

    Filled,

    Rejected,

    /// <summary>The app could not submit the order (for example, a network failure before sending).</summary>
    Failed,

    /// <summary>
    /// Submitted, but the outcome could not be confirmed. Blocks trading on that brokerage until the user
    /// reconciles it.
    /// </summary>
    Unknown,

    /// <summary>Was <see cref="Unknown"/>; the user confirmed against the brokerage that it did not fill.</summary>
    Reconciled
}

/// <summary>
/// One order the bot attempted, recorded before it is sent so that every attempt is on the ledger
/// even if the app stops mid-flight.
/// </summary>
public sealed record OrderAttempt(
    Guid Id,
    string BrokerageId,
    string StrategyId,
    OrderSide Side,
    decimal? RequestedQuantity,
    decimal? RequestedNotional,
    decimal ReferencePrice,
    string Reason,
    DateTimeOffset AttemptedAtUtc,
    OrderAttemptStatus Status,
    string? BrokerOrderId = null,
    decimal? FilledQuantity = null,
    decimal? AveragePrice = null,
    decimal? Fee = null,
    bool FeeIsEstimated = false,
    DateTimeOffset? CompletedAtUtc = null,
    string? Message = null)
{
    /// <summary>Quantity times price, before fees.</summary>
    public decimal? GrossValue => FilledQuantity * AveragePrice;
}

/// <summary>
/// A tax lot: a quantity of BTC acquired together, with its cost basis (purchase price plus fees).
/// Lots belong to one brokerage account, matching per-account basis tracking rules.
/// </summary>
public sealed record TaxLot(
    Guid Id,
    string BrokerageId,
    DateTimeOffset AcquiredAtUtc,
    decimal Quantity,
    decimal CostBasis,
    decimal RemainingQuantity,
    decimal RemainingCostBasis,
    Guid SourceAttemptId);

/// <summary>
/// The portion of a lot consumed by one sell: the unit of a Form 8949 line.
/// </summary>
public sealed record LotDisposal(
    Guid Id,
    Guid LotId,
    string BrokerageId,
    DateTimeOffset AcquiredAtUtc,
    DateTimeOffset DisposedAtUtc,
    decimal Quantity,
    decimal Proceeds,
    decimal CostBasis,
    Guid SellAttemptId)
{
    public decimal GainOrLoss => Proceeds - CostBasis;
}

/// <summary>
/// Everything a completed attempt changes, persisted atomically so the ledger and lots never disagree.
/// </summary>
public sealed record LedgerChanges(TaxLot? NewLot, IReadOnlyList<TaxLot> UpdatedLots, IReadOnlyList<LotDisposal> Disposals)
{
    public static LedgerChanges None { get; } = new(null, [], []);
}
