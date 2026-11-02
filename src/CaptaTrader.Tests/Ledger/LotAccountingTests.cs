using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Ledger;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace CaptaTrader.Tests.Ledger;

[TestClass]
public sealed class LotAccountingTests
{
    private static readonly DateTimeOffset T0 = new(2026, 3, 1, 15, 0, 0, TimeSpan.Zero);

    internal static TaxLot Lot(DateTimeOffset acquired, decimal quantity, decimal basis)
    {
        return new TaxLot(Guid.NewGuid(), "paper", acquired, quantity, basis, quantity, basis, Guid.NewGuid());
    }

    [TestMethod]
    public void MatchFifo_TakesOldestFirst_AndProratesBasis()
    {
        TaxLot newer = Lot(T0.AddHours(1), 0.002m, 120m);
        TaxLot older = Lot(T0, 0.001m, 50m);

        LotMatch match = LotAccounting.MatchFifo([newer, older], 0.0015m);

        Assert.HasCount(2, match.Allocations);
        Assert.AreEqual(older.Id, match.Allocations[0].Lot.Id);
        Assert.AreEqual(50m, match.Allocations[0].CostBasis);
        Assert.AreEqual(0.0005m, match.Allocations[1].Quantity);
        Assert.AreEqual(30m, match.Allocations[1].CostBasis);
        Assert.AreEqual(0m, match.UnmatchedQuantity);
    }

    [TestMethod]
    public void MatchFifo_ReportsShortfall()
    {
        LotMatch match = LotAccounting.MatchFifo([Lot(T0, 0.001m, 50m)], 0.003m);

        Assert.AreEqual(0.002m, match.UnmatchedQuantity);
    }

    [TestMethod]
    public void ApplyFill_Buy_CreatesLotWhoseBasisIncludesTheFee()
    {
        OrderAttempt buy = Filled(OrderSide.Buy, quantity: 0.0005m, price: 50_000m, fee: 0.10m);

        LedgerChanges changes = LotAccounting.ApplyFill(buy, []);

        Assert.IsNotNull(changes.NewLot);
        Assert.AreEqual(25.10m, changes.NewLot.CostBasis);
        Assert.AreEqual(0.0005m, changes.NewLot.RemainingQuantity);
    }

    [TestMethod]
    public void ApplyFill_Sell_SplitsNetProceedsAcrossLots_AndShrinksThem()
    {
        TaxLot first = Lot(T0, 0.001m, 50m);
        TaxLot second = Lot(T0.AddMinutes(5), 0.001m, 52m);
        OrderAttempt sell = Filled(OrderSide.Sell, quantity: 0.0015m, price: 60_000m, fee: 0.20m);

        LedgerChanges changes = LotAccounting.ApplyFill(sell, [first, second]);

        Assert.HasCount(2, changes.Disposals);
        Assert.AreEqual(89.80m, changes.Disposals.Sum(disposal => disposal.Proceeds));
        Assert.AreEqual(76m, changes.Disposals.Sum(disposal => disposal.CostBasis));
        Assert.AreEqual(0m, changes.UpdatedLots.Single(lot => lot.Id == first.Id).RemainingQuantity);
        Assert.AreEqual(0.0005m, changes.UpdatedLots.Single(lot => lot.Id == second.Id).RemainingQuantity);
        Assert.AreEqual(26m, changes.UpdatedLots.Single(lot => lot.Id == second.Id).RemainingCostBasis);
    }

    [TestMethod]
    public void ApplyFill_IgnoresUnfilledAttempts()
    {
        OrderAttempt rejected = Filled(OrderSide.Buy, 0.001m, 50_000m, 0m) with { Status = OrderAttemptStatus.Rejected };

        Assert.AreSame(LedgerChanges.None, LotAccounting.ApplyFill(rejected, []));
    }

    [TestMethod]
    [DataRow("2025-03-01", "2026-03-01", false)]
    [DataRow("2025-03-01", "2026-03-02", true)]
    [DataRow("2024-02-29", "2025-02-28", false)]
    [DataRow("2024-02-29", "2025-03-01", true)]
    public void IsLongTerm_RequiresMoreThanOneYear(string acquired, string disposed, bool expected)
    {
        Assert.AreEqual(expected, LotAccounting.IsLongTerm(DateOnly.Parse(acquired), DateOnly.Parse(disposed)));
    }

    internal static OrderAttempt Filled(OrderSide side, decimal quantity, decimal price, decimal fee, DateTimeOffset? at = null)
    {
        DateTimeOffset time = at ?? T0.AddDays(1);

        return new OrderAttempt(
            Guid.NewGuid(), "paper", "test", side,
            side == OrderSide.Sell ? quantity : null,
            side == OrderSide.Buy ? (quantity * price) + fee : null,
            price, "test", time, OrderAttemptStatus.Filled,
            "broker-1", quantity, price, fee, false, time, null);
    }
}
