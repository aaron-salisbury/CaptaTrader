using CaptaTrader.Business.Modules.Brokerage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaptaTrader.Business.Modules.Ledger;

/// <summary>
/// The part of one lot a sale would consume, with that part's share of the lot's basis.
/// </summary>
public sealed record LotAllocation(TaxLot Lot, decimal Quantity, decimal CostBasis);

/// <summary>
/// Result of matching a sale quantity to lots. <see cref="UnmatchedQuantity"/> is non-zero when the
/// app's own lots cannot cover the sale.
/// </summary>
public sealed record LotMatch(IReadOnlyList<LotAllocation> Allocations, decimal UnmatchedQuantity)
{
    public decimal MatchedQuantity => Allocations.Sum(allocation => allocation.Quantity);

    public decimal CostBasis => Allocations.Sum(allocation => allocation.CostBasis);
}

/// <summary>
/// First-in, first-out lot accounting.
/// </summary>
/// <remarks>
/// FIFO is the IRS default for digital assets held at a broker unless specific lots are identified to
/// the broker no later than the time of sale (Treas. Reg. §1.1012-1(j), Rev. Proc. 2024-28). Most
/// brokerage APIs cannot receive lot instructions, so the app models what the brokerage will report.
/// See docs/tax_considerations.md.
/// </remarks>
public static class LotAccounting
{
    public static LotMatch MatchFifo(IEnumerable<TaxLot> openLots, decimal quantity)
    {
        ArgumentNullException.ThrowIfNull(openLots);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        List<LotAllocation> allocations = [];
        decimal remaining = quantity;

        foreach (TaxLot lot in openLots.Where(lot => lot.RemainingQuantity > 0m).OrderBy(lot => lot.AcquiredAtUtc).ThenBy(lot => lot.Id))
        {
            if (remaining <= 0m)
            {
                break;
            }

            decimal take = Math.Min(remaining, lot.RemainingQuantity);
            decimal basis = take == lot.RemainingQuantity
                ? lot.RemainingCostBasis
                : lot.RemainingCostBasis * take / lot.RemainingQuantity;

            allocations.Add(new LotAllocation(lot, take, basis));
            remaining -= take;
        }

        return new LotMatch(allocations, remaining);
    }

    /// <summary>
    /// Computes the lot changes for a filled attempt: a new lot for a buy, or FIFO disposals for a sell.
    /// </summary>
    public static LedgerChanges ApplyFill(OrderAttempt attempt, IReadOnlyList<TaxLot> openLots)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(openLots);

        if (attempt.Status != OrderAttemptStatus.Filled || attempt.FilledQuantity is not > 0m || attempt.AveragePrice is null)
        {
            return LedgerChanges.None;
        }

        decimal quantity = attempt.FilledQuantity.Value;
        decimal gross = quantity * attempt.AveragePrice.Value;
        decimal fee = attempt.Fee ?? 0m;
        DateTimeOffset filledAt = attempt.CompletedAtUtc ?? attempt.AttemptedAtUtc;

        if (attempt.Side == OrderSide.Buy)
        {
            decimal basis = gross + fee;
            TaxLot lot = new(Guid.NewGuid(), attempt.BrokerageId, filledAt, quantity, basis, quantity, basis, attempt.Id);
            return new LedgerChanges(lot, [], []);
        }

        LotMatch match = MatchFifo(openLots, quantity);

        if (match.UnmatchedQuantity > 0m)
        {
            // Pre-trade checks prevent this; reaching it means the brokerage filled more than the app owns.
            throw new InvalidOperationException("A sell filled more BTC than the app's open lots hold; reconcile with the brokerage.");
        }

        decimal proceeds = gross - fee;
        List<TaxLot> updated = [];
        List<LotDisposal> disposals = [];

        foreach (LotAllocation allocation in match.Allocations)
        {
            decimal share = proceeds * allocation.Quantity / quantity;
            disposals.Add(new LotDisposal(
                Guid.NewGuid(),
                allocation.Lot.Id,
                attempt.BrokerageId,
                allocation.Lot.AcquiredAtUtc,
                filledAt,
                allocation.Quantity,
                share,
                allocation.CostBasis,
                attempt.Id));

            updated.Add(allocation.Lot with
            {
                RemainingQuantity = allocation.Lot.RemainingQuantity - allocation.Quantity,
                RemainingCostBasis = allocation.Lot.RemainingCostBasis - allocation.CostBasis
            });
        }

        return new LedgerChanges(null, updated, disposals);
    }

    /// <summary>
    /// Long-term treatment requires holding more than one year. The holding period starts the day
    /// after acquisition, so an asset bought on March 1 is long-term when sold on March 2 of the next year.
    /// </summary>
    public static bool IsLongTerm(DateOnly acquired, DateOnly disposed)
    {
        return disposed > acquired.AddYears(1);
    }
}
