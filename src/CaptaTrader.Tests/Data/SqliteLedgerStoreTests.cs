using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.Settings;
using CaptaTrader.Tests.Ledger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaptaTrader.Tests.Data;

[TestClass]
public sealed class SqliteLedgerStoreTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Attempt_RoundTripsExactDecimalsAndInstants()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);
        ILedgerStore ledger = services.GetRequiredService<ILedgerStore>();

        OrderAttempt attempt = LotAccountingTests.Filled(OrderSide.Buy, 0.00049875m, 50_123.456789m, 0.0625m, new DateTimeOffset(2026, 6, 1, 12, 0, 0, 123, TimeSpan.Zero)) with
        {
            Status = OrderAttemptStatus.Pending
        };

        await ledger.InsertAttemptAsync(attempt, TestContext.CancellationToken);
        OrderAttempt stored = (await ledger.GetRecentAttemptsAsync(1, TestContext.CancellationToken)).Single();

        Assert.AreEqual(attempt, stored);
    }

    [TestMethod]
    public async Task CompleteAttempt_PersistsLotsAndDisposalsTogether()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);
        ILedgerStore ledger = services.GetRequiredService<ILedgerStore>();

        OrderAttempt buy = LotAccountingTests.Filled(OrderSide.Buy, 0.001m, 50_000m, 0.10m);
        await ledger.InsertAttemptAsync(buy with { Status = OrderAttemptStatus.Pending }, TestContext.CancellationToken);
        await ledger.CompleteAttemptAsync(buy, LotAccounting.ApplyFill(buy, []), TestContext.CancellationToken);

        IReadOnlyList<TaxLot> lots = await ledger.GetOpenLotsAsync("paper", TestContext.CancellationToken);
        Assert.HasCount(1, lots);
        Assert.AreEqual(50.10m, lots[0].CostBasis);

        OrderAttempt sell = LotAccountingTests.Filled(OrderSide.Sell, 0.001m, 55_000m, 0.14m, buy.AttemptedAtUtc.AddHours(1));
        await ledger.InsertAttemptAsync(sell with { Status = OrderAttemptStatus.Pending }, TestContext.CancellationToken);
        await ledger.CompleteAttemptAsync(sell, LotAccounting.ApplyFill(sell, lots), TestContext.CancellationToken);

        Assert.IsEmpty(await ledger.GetOpenLotsAsync("paper", TestContext.CancellationToken));
        LotDisposal disposal = (await ledger.GetDisposalsAsync(DateTimeOffset.MinValue, DateTimeOffset.MaxValue, TestContext.CancellationToken)).Single();
        Assert.AreEqual(54.86m, disposal.Proceeds);
        Assert.AreEqual(4.76m, disposal.GainOrLoss);
        Assert.AreEqual(lots[0].AcquiredAtUtc, disposal.AcquiredAtUtc);
    }

    [TestMethod]
    public async Task CompleteAttempt_ForUnknownAttempt_RollsBackLotChanges()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);
        ILedgerStore ledger = services.GetRequiredService<ILedgerStore>();
        OrderAttempt neverInserted = LotAccountingTests.Filled(OrderSide.Buy, 0.001m, 50_000m, 0.10m);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            ledger.CompleteAttemptAsync(neverInserted, LotAccounting.ApplyFill(neverInserted, []), TestContext.CancellationToken));

        Assert.IsEmpty(await ledger.GetOpenLotsAsync("paper", TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task AbandonedPendingAttempts_AreMarkedUnknown()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);
        ILedgerStore ledger = services.GetRequiredService<ILedgerStore>();
        OrderAttempt pending = LotAccountingTests.Filled(OrderSide.Buy, 0.001m, 50_000m, 0.10m) with { Status = OrderAttemptStatus.Pending };

        await ledger.InsertAttemptAsync(pending, TestContext.CancellationToken);
        int marked = await ledger.MarkAbandonedPendingAsUnknownAsync("reconcile", TestContext.CancellationToken);

        Assert.AreEqual(1, marked);
        Assert.AreEqual(OrderAttemptStatus.Unknown, (await ledger.GetRecentAttemptsAsync(1, TestContext.CancellationToken)).Single().Status);
    }

    [TestMethod]
    public async Task TradingSettings_RoundTripAndValidate()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);
        ManageTradingSettings settings = services.GetRequiredService<ManageTradingSettings>();

        Assert.AreEqual(TradingSettings.Default, await settings.GetAsync(TestContext.CancellationToken));

        TradingSettings changed = TradingSettings.Default with { MaxBuyNotional = 10.5m, ShortTermTaxRate = 0.37m };
        Assert.IsTrue((await settings.SaveAsync(changed, TestContext.CancellationToken)).IsSuccess);
        Assert.AreEqual(changed, await settings.GetAsync(TestContext.CancellationToken));

        Assert.IsFalse((await settings.SaveAsync(changed with { MinimumNetProfit = 0.001m }, TestContext.CancellationToken)).IsSuccess);
        Assert.IsFalse((await settings.SaveAsync(changed with { ShortTermTaxRate = 1.2m }, TestContext.CancellationToken)).IsSuccess);
    }
}
