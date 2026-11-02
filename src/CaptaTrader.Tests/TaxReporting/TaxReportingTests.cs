using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Business.Modules.TaxReporting;
using CaptaTrader.Business.Modules.UserAccess;
using CaptaTrader.Integrations.TaxReporting;
using CaptaTrader.Tests.Ledger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CaptaTrader.Tests.TaxReporting;

[TestClass]
public sealed class TaxReportingTests
{
    private const string REAL_BROKERAGE = "real-broker";

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task Report_ExcludesSimulatedAccounts_OutsideDemo()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await CreateAsync(directory.Path, TimeZoneInfo.Utc);
        DateTimeOffset buyTime = new(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);

        await RoundTripAsync(services, "paper", buyTime);
        await RoundTripAsync(services, REAL_BROKERAGE, buyTime);

        services.GetRequiredService<UserSession>().SignOut();
        TaxYearReport real = await services.GetRequiredService<BuildTaxReports>().BuildAsync(2026, TestContext.CancellationToken);
        Assert.HasCount(1, real.Disposals);
        Assert.AreEqual("Real broker", real.Disposals[0].Brokerage);
        Assert.IsFalse(real.IncludesSimulatedAccounts);

        services.GetRequiredService<UserSession>().SignInDemo();
        TaxYearReport demo = await services.GetRequiredService<BuildTaxReports>().BuildAsync(2026, TestContext.CancellationToken);
        Assert.HasCount(2, demo.Disposals);
    }

    [TestMethod]
    public async Task TaxYear_FollowsTheLocalTimeZone()
    {
        using TestDirectory directory = new();
        TimeZoneInfo newYork = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "Eastern Standard Time" : "America/New_York");
        await using ServiceProvider services = await CreateAsync(directory.Path, newYork);

        // 02:00 UTC on January 1 is still December 31 in New York.
        await RoundTripAsync(services, REAL_BROKERAGE, new DateTimeOffset(2026, 12, 31, 20, 0, 0, TimeSpan.Zero), sellAfter: TimeSpan.FromHours(6));
        BuildTaxReports reports = services.GetRequiredService<BuildTaxReports>();

        Assert.HasCount(1, (await reports.BuildAsync(2026, TestContext.CancellationToken)).Disposals);
        Assert.IsEmpty((await reports.BuildAsync(2027, TestContext.CancellationToken)).Disposals);
        Assert.AreSequenceEqual(new[] { 2026 }, (await reports.GetYearsAsync(TestContext.CancellationToken)).ToArray());
    }

    [TestMethod]
    public void Csv_UsesForm8949Columns_AndEscapesFields()
    {
        TaxReportRow row = new("0.001 BTC", new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 2), 54.855m, 50.1m, 4.755m, false, "Broker, \"Inc\"", Guid.Empty);
        TaxYearReport report = new(2026, [row], [], new TaxTermTotals(54.855m, 50.1m, 4.755m), new TaxTermTotals(0m, 0m, 0m), false);

        string[] lines = CsvTaxReportWriter.BuildDisposals(report).Split("\r\n");

        Assert.AreEqual("Description,DateAcquired,DateSold,Proceeds,CostBasis,AdjustmentCode,AdjustmentAmount,GainOrLoss,Term,Brokerage,LotId", lines[0]);
        Assert.AreEqual($"0.001 BTC,2026-02-01,2026-02-02,54.86,50.10,,,4.76,Short-term,\"Broker, \"\"Inc\"\"\",{Guid.Empty:D}", lines[1]);
    }

    [TestMethod]
    public async Task Export_WritesOneFilePairPerYear()
    {
        using TestDirectory directory = new();
        await using ServiceProvider services = await CreateAsync(directory.Path, TimeZoneInfo.Utc);
        await RoundTripAsync(services, REAL_BROKERAGE, new DateTimeOffset(2026, 3, 1, 12, 0, 0, TimeSpan.Zero));

        TaxReportFiles files = (await services.GetRequiredService<BuildTaxReports>().ExportAsync(2026, TestContext.CancellationToken)).Value!;

        Assert.EndsWith("captatrader-2026-form8949.csv", files.DisposalsPath);
        Assert.HasCount(2, File.ReadAllLines(files.DisposalsPath));
        Assert.HasCount(3, File.ReadAllLines(files.TransactionsPath));
    }

    private static async Task RoundTripAsync(ServiceProvider services, string brokerageId, DateTimeOffset buyTime, TimeSpan? sellAfter = null)
    {
        ILedgerStore ledger = services.GetRequiredService<ILedgerStore>();
        OrderAttempt buy = LotAccountingTests.Filled(OrderSide.Buy, 0.001m, 50_000m, 0.10m, buyTime) with { BrokerageId = brokerageId };
        await ledger.InsertAttemptAsync(buy with { Status = OrderAttemptStatus.Pending }, default);
        await ledger.CompleteAttemptAsync(buy, LotAccounting.ApplyFill(buy, []), default);

        IReadOnlyList<TaxLot> lots = await ledger.GetOpenLotsAsync(brokerageId, default);
        OrderAttempt sell = LotAccountingTests.Filled(OrderSide.Sell, 0.001m, 55_000m, 0.14m, buyTime + (sellAfter ?? TimeSpan.FromDays(1))) with { BrokerageId = brokerageId };
        await ledger.InsertAttemptAsync(sell with { Status = OrderAttemptStatus.Pending }, default);
        await ledger.CompleteAttemptAsync(sell, LotAccounting.ApplyFill(sell, lots), default);
    }

    private static Task<ServiceProvider> CreateAsync(string directory, TimeZoneInfo zone)
    {
        return TestServices.CreateAsync(directory, services =>
        {
            services.AddSingleton<TimeProvider>(new ZonedTimeProvider(zone));
            services.AddSingleton<IBrokerageProvider>(new NamedBrokerage(REAL_BROKERAGE, "Real broker"));
        });
    }

    private sealed class NamedBrokerage : IBrokerageProvider
    {
        internal NamedBrokerage(string id, string name)
        {
            Descriptor = new ProviderDescriptor(id, ProviderKind.Brokerage, name, string.Empty, IsSimulated: false, []);
        }

        public ProviderDescriptor Descriptor { get; }

        public BrokerageRules Rules { get; } = new(new FeeSchedule(0m, 0m), 1m, 8, 0m);

        public IBrokerage Connect(ProviderConnection connection)
        {
            throw new NotSupportedException();
        }
    }
}
