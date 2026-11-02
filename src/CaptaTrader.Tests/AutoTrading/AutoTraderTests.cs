using CaptaTrader.Business.Modules.AutoTrading;
using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Business.Modules.Settings;
using CaptaTrader.Business.Modules.Strategies;
using CaptaTrader.Business.Modules.TaxReporting;
using CaptaTrader.Business.Modules.UserAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Tests.AutoTrading;

[TestClass]
public sealed class AutoTraderTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Start_RequiresSignIn()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);

        CapabilityResult<TradingStatus> result = await services.GetRequiredService<AutoTrader>().StartAsync(TestContext.CancellationToken);

        Assert.AreEqual(CapabilityStatus.Unauthorized, result.Status);
    }

    [TestMethod]
    public async Task DemoReplay_TradesManyTimesAndKeepsBooksConsistent()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);
        services.GetRequiredService<UserSession>().SignInDemo();
        Assert.IsTrue((await services.GetRequiredService<ConfigureProviders>().SelectSimulatedProvidersAsync(TestContext.CancellationToken)).IsSuccess);

        AutoTrader autoTrader = services.GetRequiredService<AutoTrader>();
        CapabilityResult<TradingStatus> started = await autoTrader.StartAsync(TestContext.CancellationToken);
        Assert.IsTrue(started.IsSuccess, started.Message);

        await WaitUntilStoppedAsync(autoTrader);
        List<TradingEvent> events = Drain(autoTrader);

        ILedgerStore ledger = services.GetRequiredService<ILedgerStore>();
        IReadOnlyList<OrderAttempt> attempts = await ledger.GetRecentAttemptsAsync(10_000, TestContext.CancellationToken);
        IReadOnlyList<TaxLot> openLots = await ledger.GetOpenLotsAsync("paper", TestContext.CancellationToken);
        PaperAccount paper = await services.GetRequiredService<IPaperAccountStore>().GetOrCreateAsync(0m, TestContext.CancellationToken);

        // The strategy is random; across many runs this day yields roughly 30 to 45 attempts.
        Assert.IsGreaterThanOrEqualTo(25, attempts.Count, "The volatile demo day should produce many attempts.");
        Assert.IsTrue(attempts.All(attempt => attempt.Status == OrderAttemptStatus.Filled));
        Assert.IsInstanceOfType<SessionStopped>(events[^1]);

        // The app's lots and the brokerage's balance must agree to the satoshi.
        Assert.AreEqual(paper.Bitcoin, openLots.Sum(lot => lot.RemainingQuantity));

        // Every sell met the one-cent rule: sells are only proposed when the after-fee, after-tax estimate clears it.
        IReadOnlyList<LotDisposal> disposals = await ledger.GetDisposalsAsync(DateTimeOffset.MinValue, DateTimeOffset.MaxValue, TestContext.CancellationToken);
        foreach (IGrouping<Guid, LotDisposal> sale in disposals.GroupBy(disposal => disposal.SellAttemptId))
        {
            Assert.IsGreaterThan(0m, sale.Sum(disposal => disposal.GainOrLoss));
        }

        // Cash is conserved: starting cash + proceeds - purchase costs.
        decimal bought = attempts.Where(attempt => attempt.Side == OrderSide.Buy).Sum(attempt => (attempt.GrossValue ?? 0m) + (attempt.Fee ?? 0m));
        decimal sold = attempts.Where(attempt => attempt.Side == OrderSide.Sell).Sum(attempt => (attempt.GrossValue ?? 0m) - (attempt.Fee ?? 0m));
        Assert.AreEqual(1_000m + sold - bought, paper.CashUsd);

        TaxYearReport report = await services.GetRequiredService<BuildTaxReports>().BuildAsync(2024, TestContext.CancellationToken);
        Assert.HasCount(disposals.Count, report.Disposals);
        Assert.AreEqual(disposals.Sum(disposal => disposal.GainOrLoss), report.NetGainOrLoss);
    }

    [TestMethod]
    public async Task Stop_EndsSessionAndRecordsReason()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);
        services.GetRequiredService<UserSession>().SignInDemo();
        await services.GetRequiredService<ConfigureProviders>().SelectSimulatedProvidersAsync(TestContext.CancellationToken);
        AutoTrader autoTrader = services.GetRequiredService<AutoTrader>();

        Assert.IsTrue((await autoTrader.StartAsync(TestContext.CancellationToken)).IsSuccess);
        Assert.AreEqual(CapabilityStatus.Busy, (await autoTrader.StartAsync(TestContext.CancellationToken)).Status);

        await autoTrader.StopAsync();

        Assert.IsFalse(autoTrader.IsRunning);
        SessionStopped stopped = Drain(autoTrader).OfType<SessionStopped>().Single();
        Assert.IsTrue(stopped.Reason is "Stopped by user." or "Market data ended.");
    }

    [TestMethod]
    public async Task UnknownOutcome_HaltsSession_AndBlocksStartUntilReconciled()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(
            directory.Path,
            collection => collection.AddSingleton<IBrokerageProvider, AmbiguousBrokerageProvider>());
        services.GetRequiredService<UserSession>().SignInDemo();
        ConfigureProviders configureProviders = services.GetRequiredService<ConfigureProviders>();
        await configureProviders.SelectSimulatedProvidersAsync(TestContext.CancellationToken);
        Assert.IsTrue((await configureProviders.SelectAsync(ProviderKind.Brokerage, AmbiguousBrokerageProvider.ID, TestContext.CancellationToken)).IsSuccess);
        AutoTrader autoTrader = services.GetRequiredService<AutoTrader>();

        Assert.IsTrue((await autoTrader.StartAsync(TestContext.CancellationToken)).IsSuccess);
        await WaitUntilStoppedAsync(autoTrader);

        IReadOnlyList<OrderAttempt> attempts = await services.GetRequiredService<ILedgerStore>().GetRecentAttemptsAsync(100, TestContext.CancellationToken);
        Assert.HasCount(1, attempts, "No further orders may be placed after an unknown outcome.");
        Assert.AreEqual(OrderAttemptStatus.Unknown, attempts[0].Status);
        StringAssert.Contains(Drain(autoTrader).OfType<SessionStopped>().Single().Reason, "unknown");

        Assert.AreEqual(CapabilityStatus.Conflict, (await autoTrader.StartAsync(TestContext.CancellationToken)).Status);

        // The brokerage shows the buy did fill: entering it must create the lot, just as a live fill would.
        OrderAttempt unknownBuy = Assert.ContainsSingle((await autoTrader.GetUnknownOrdersAsync(TestContext.CancellationToken)).Value!);
        Assert.AreEqual(
            CapabilityStatus.InvalidRequest,
            (await autoTrader.ReconcileUnknownOrderAsync(Guid.NewGuid(), OrderReconciliation.NotFilled, TestContext.CancellationToken)).Status);

        DateTimeOffset filledAt = unknownBuy.AttemptedAtUtc.AddMinutes(3);
        Assert.AreEqual(
            CapabilityStatus.InvalidRequest,
            (await autoTrader.ReconcileUnknownOrderAsync(unknownBuy.Id, OrderReconciliation.Fill(0.004m, 60_000m, 0.06m, filledAt), TestContext.CancellationToken)).Status,
            "A mistyped quantity costing ten times the request must be refused.");

        CapabilityResult<OrderAttempt> reconciled = await autoTrader.ReconcileUnknownOrderAsync(
            unknownBuy.Id, OrderReconciliation.Fill(0.0004m, 60_000m, 0.06m, filledAt), TestContext.CancellationToken);

        Assert.IsTrue(reconciled.IsSuccess, reconciled.Message);
        Assert.AreEqual(OrderAttemptStatus.Filled, reconciled.Value!.Status);
        TaxLot lot = Assert.ContainsSingle(await services.GetRequiredService<ILedgerStore>().GetOpenLotsAsync(AmbiguousBrokerageProvider.ID, TestContext.CancellationToken));
        Assert.AreEqual(0.0004m, lot.Quantity);
        Assert.AreEqual(24.06m, lot.CostBasis);
        Assert.AreEqual(filledAt, lot.AcquiredAtUtc, "The lot is acquired when the brokerage filled it, not when the order was sent.");

        // A second completion of the same order (as from another process) must not record the fill again.
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => services.GetRequiredService<ILedgerStore>()
            .CompleteAttemptAsync(reconciled.Value, LedgerChanges.None, TestContext.CancellationToken));

        Assert.IsTrue((await autoTrader.StartAsync(TestContext.CancellationToken)).IsSuccess);
        await autoTrader.StopAsync();
    }

    [TestMethod]
    public async Task HourlyOrderLimit_CountsAttemptsFromEarlierSessions()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await TestServices.CreateAsync(
            directory.Path,
            collection => collection.AddSingleton<ITradingStrategy, AlwaysBuyStrategy>());
        services.GetRequiredService<UserSession>().SignInDemo();
        await services.GetRequiredService<ConfigureProviders>().SelectSimulatedProvidersAsync(TestContext.CancellationToken);
        ManageTradingSettings settings = services.GetRequiredService<ManageTradingSettings>();
        TradingSettings current = await settings.GetAsync(TestContext.CancellationToken);
        CapabilityResult<TradingSettings> saved = await settings.SaveAsync(
            current with { StrategyId = AlwaysBuyStrategy.ID, MaxOrdersPerHour = 1 },
            TestContext.CancellationToken);
        Assert.IsTrue(saved.IsSuccess, saved.Message);

        // An earlier session used the hour's only order 30 seconds before the replay's first tick.
        ILedgerStore ledger = services.GetRequiredService<ILedgerStore>();
        DateTimeOffset earlier = new(2024, 8, 4, 23, 59, 30, TimeSpan.Zero);
        OrderAttempt previous = new(Guid.NewGuid(), "paper", "random-volatility", OrderSide.Buy, null, 25m, 58_000m, "Earlier session.", earlier, OrderAttemptStatus.Pending);
        await ledger.InsertAttemptAsync(previous, TestContext.CancellationToken);
        await ledger.CompleteAttemptAsync(previous with { Status = OrderAttemptStatus.Rejected, CompletedAtUtc = earlier }, LedgerChanges.None, TestContext.CancellationToken);

        AutoTrader autoTrader = services.GetRequiredService<AutoTrader>();
        Assert.IsTrue((await autoTrader.StartAsync(TestContext.CancellationToken)).IsSuccess);
        await WaitUntilStoppedAsync(autoTrader);

        // The strategy wants to buy on every tick, so the first new order shows exactly when the budget freed up.
        IReadOnlyList<OrderAttempt> attempts = await ledger.GetRecentAttemptsAsync(10_000, TestContext.CancellationToken);
        DateTimeOffset firstNewOrder = attempts.Where(attempt => attempt.Id != previous.Id).Min(attempt => attempt.AttemptedAtUtc);
        Assert.IsGreaterThanOrEqualTo(earlier.AddHours(1), firstNewOrder, "No order may be placed until the earlier session's order leaves the rolling hour.");
    }

    [TestMethod]
    public void SimulatedPrices_CannotDriveARealBrokerage()
    {
        ProviderDescriptor realBrokerage = new("real", ProviderKind.Brokerage, "Real", string.Empty, IsSimulated: false, []);
        ProviderDescriptor paperBrokerage = new("paper", ProviderKind.Brokerage, "Paper", string.Empty, IsSimulated: true, []);
        ProviderDescriptor replay = new("replay", ProviderKind.MarketData, "Replay", string.Empty, IsSimulated: true, []);
        ProviderDescriptor live = new("live", ProviderKind.MarketData, "Live", string.Empty, IsSimulated: false, []);

        Assert.IsNotNull(AutoTrader.ValidateCombination(realBrokerage, replay, isDemo: false));
        Assert.IsNotNull(AutoTrader.ValidateCombination(realBrokerage, live, isDemo: true));
        Assert.IsNull(AutoTrader.ValidateCombination(paperBrokerage, replay, isDemo: true));
        Assert.IsNull(AutoTrader.ValidateCombination(realBrokerage, live, isDemo: false));
    }

    private async Task WaitUntilStoppedAsync(AutoTrader autoTrader)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (autoTrader.IsRunning)
        {
            Assert.IsLessThan(TimeSpan.FromMinutes(2), stopwatch.Elapsed, "Replay did not finish in time.");
            await Task.Delay(50, TestContext.CancellationToken);
        }

        // StopAsync joins the session task so every final event is published.
        await autoTrader.StopAsync();
    }

    private static List<TradingEvent> Drain(AutoTrader autoTrader)
    {
        List<TradingEvent> events = [];

        while (autoTrader.Events.TryRead(out TradingEvent? tradingEvent))
        {
            events.Add(tradingEvent);
        }

        return events;
    }

    private sealed class AlwaysBuyStrategy : ITradingStrategy
    {
        internal const string ID = "always-buy";

        public StrategyDescriptor Descriptor { get; } = new(ID, "Always buy", string.Empty);

        public void Reset()
        {
        }

        public TradeDecision Evaluate(StrategyContext context)
        {
            return TradeDecision.Buy(25m, "Test: buy on every tick.");
        }
    }

    /// <summary>A simulated brokerage whose every order outcome is ambiguous, like a timeout after sending.</summary>
    private sealed class AmbiguousBrokerageProvider : IBrokerageProvider, IBrokerage
    {
        internal const string ID = "ambiguous";

        public ProviderDescriptor Descriptor { get; } = new(ID, ProviderKind.Brokerage, "Ambiguous", string.Empty, IsSimulated: true, []);

        public BrokerageRules Rules { get; } = new(new FeeSchedule(0.0015m, 0.0025m), 1m, 8, 0.0005m);

        public IBrokerage Connect(ProviderConnection connection)
        {
            return this;
        }

        public Task<AccountSnapshot> GetAccountAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AccountSnapshot(1_000m, 0m, DateTimeOffset.UtcNow));
        }

        public Task<OrderResult> SubmitMarketOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(OrderResult.Unknown("Timed out after sending."));
        }
    }
}
