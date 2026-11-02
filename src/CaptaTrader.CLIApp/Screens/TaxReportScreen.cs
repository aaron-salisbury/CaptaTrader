using CaptaTrader.Business.Modules.AutoTrading;
using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.TaxReporting;
using CaptaTrader.Business.Modules.UserAccess;
using CaptaTrader.CLIApp.Base;
using PrettyConsole;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace CaptaTrader.CLIApp.Screens;

internal sealed class TaxReportScreen
{
    private const string BACK = "Back";

    private readonly BuildTaxReports _taxReports;
    private readonly AutoTrader _autoTrader;
    private readonly UserSession _session;

    public TaxReportScreen(BuildTaxReports taxReports, AutoTrader autoTrader, UserSession session)
    {
        _taxReports = taxReports ?? throw new ArgumentNullException(nameof(taxReports));
        _autoTrader = autoTrader ?? throw new ArgumentNullException(nameof(autoTrader));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    internal async Task RunAsync()
    {
        Ui.Screen("Tax reports", _autoTrader.Status, _session.IsDemo);
        Ui.Hint("Estimates for your records and your tax preparer; not tax advice. Your brokerage's Form 1099-DA");
        Ui.Hint("is what the IRS receives, so reconcile against it. See docs/tax_considerations.md.");
        Console.NewLine();

        IReadOnlyList<int> years = await _taxReports.GetYearsAsync();

        if (years.Count == 0)
        {
            Ui.Hint("No filled trades yet.");
            Ui.Pause();
            return;
        }

        string? choice = Ui.Choose("Which tax year?", [.. years.Select(year => year.ToString(CultureInfo.InvariantCulture)), BACK]);

        if (choice is null || choice == BACK)
        {
            return;
        }

        int selectedYear = int.Parse(choice, CultureInfo.InvariantCulture);
        TaxYearReport report = await _taxReports.BuildAsync(selectedYear);

        Console.NewLine();
        Console.WriteLineInterpolated($"{Markup.Bold}{selectedYear}{Markup.Reset}: {report.Disposals.Count} disposal line(s) from {report.Transactions.Count} filled order(s).");

        if (!report.IncludesSimulatedAccounts)
        {
            Ui.Hint("Paper (simulated) accounts are excluded.");
        }

        Ui.Table(
            ["Term", "Proceeds", "Cost basis", "Gain or loss"],
            [
                ["Short-term", Ui.Usd(report.ShortTerm.Proceeds), Ui.Usd(report.ShortTerm.CostBasis), Ui.SignedUsd(report.ShortTerm.GainOrLoss)],
                ["Long-term", Ui.Usd(report.LongTerm.Proceeds), Ui.Usd(report.LongTerm.CostBasis), Ui.SignedUsd(report.LongTerm.GainOrLoss)],
                ["Net", string.Empty, string.Empty, Ui.SignedUsd(report.NetGainOrLoss)]
            ]);

        if (report.NetGainOrLoss < 0m)
        {
            Ui.Hint("A net capital loss offsets up to $3,000 of other income per year ($1,500 married filing separately);");
            Ui.Hint("the rest carries forward.");
        }

        CapabilityResult<TaxReportFiles> export = await Ui.SpinAsync(_taxReports.ExportAsync(selectedYear), "Writing CSV files…");

        if (export.IsSuccess)
        {
            Ui.Success("Saved:");
            Ui.Hint("  " + export.Value!.DisposalsPath);
            Ui.Hint("  " + export.Value.TransactionsPath);
        }
        else
        {
            Ui.Failure(export);
        }

        Ui.Pause();
    }
}
