using CaptaTrader.Business.Modules.AutoTrading;
using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.UserAccess;
using CaptaTrader.CLIApp.Base;
using PrettyConsole;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace CaptaTrader.CLIApp.Screens;

internal sealed class MainMenuScreen
{
    private const string START = "Start auto-trading";
    private const string MONITOR = "Open live trading monitor";
    private const string STOP = "Stop auto-trading";
    private const string LEDGER = "Recent trades";
    private const string TAX = "Tax reports";
    private const string SETTINGS = "Settings";
    private const string EXIT = "Exit";

    private readonly AutoTrader _autoTrader;
    private readonly UserSession _session;
    private readonly TradingMonitorScreen _monitor;
    private readonly LedgerScreen _ledger;
    private readonly TaxReportScreen _taxReports;
    private readonly SettingsScreen _settings;

    public MainMenuScreen(AutoTrader autoTrader, UserSession session, TradingMonitorScreen monitor, LedgerScreen ledger, TaxReportScreen taxReports, SettingsScreen settings)
    {
        _autoTrader = autoTrader ?? throw new ArgumentNullException(nameof(autoTrader));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        _taxReports = taxReports ?? throw new ArgumentNullException(nameof(taxReports));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    internal async Task RunAsync()
    {
        if (_session.IsDemo)
        {
            await StartDemoAsync();
        }

        while (true)
        {
            Ui.Screen($"Main menu · {_session.UserName}", _autoTrader.Status, _session.IsDemo);

            List<string> items = _autoTrader.IsRunning ? [MONITOR, STOP] : [START];
            items.AddRange([LEDGER, TAX, SETTINGS, EXIT]);

            switch (Ui.Choose("What would you like to do?", items))
            {
                case START:
                    await StartAsync();
                    break;
                case MONITOR:
                    await _monitor.RunAsync();
                    break;
                case STOP:
                    await StopAsync();
                    break;
                case LEDGER:
                    await _ledger.RunAsync();
                    break;
                case TAX:
                    await _taxReports.RunAsync();
                    break;
                case SETTINGS:
                    await _settings.RunAsync();
                    break;
                case EXIT:
                    if (await ConfirmExitAsync())
                    {
                        return;
                    }

                    break;
            }
        }
    }

    private async Task StartDemoAsync()
    {
        Ui.Clear();
        Ui.Banner();
        Console.WriteLineInterpolated($"{Color.Black}{Color.YellowBackground} DEMO {Color.Default} Replaying {Markup.Bold}August 5, 2024{Markup.Reset}, when BTC fell from about $58k to $49k and rebounded.");
        Ui.Hint("A paper account with $1,000 trades the placeholder strategy against that day at high speed.");
        Ui.Hint("Nothing here touches a real account, the real database, or real tax files.");
        Ui.Pause();

        await StartAsync();
    }

    private async Task StartAsync()
    {
        Console.NewLine();
        CapabilityResult<TradingStatus> result = await Ui.SpinAsync(_autoTrader.StartAsync(), "Connecting to the brokerage and market data…");

        if (result.Status == CapabilityStatus.Conflict)
        {
            Ui.Warning(result.Message ?? "Some orders have unknown outcomes.");

            if (!await ReconcileUnknownOrdersAsync())
            {
                Ui.Pause();
                return;
            }

            result = await Ui.SpinAsync(_autoTrader.StartAsync(), "Connecting to the brokerage and market data…");
        }

        if (!result.IsSuccess)
        {
            Ui.Failure(result);
            Ui.Pause();
            return;
        }

        await _monitor.RunAsync();
    }

    /// <summary>
    /// Walks the user through each unknown order, oldest first, recording what the brokerage says happened.
    /// </summary>
    /// <returns>True when every unknown order was reconciled.</returns>
    private async Task<bool> ReconcileUnknownOrdersAsync()
    {
        const string FILLED = "It filled; enter the fill from the brokerage";
        const string NOT_FILLED = "It is canceled, expired, or rejected with nothing filled";
        const string LATER = "Not now";

        CapabilityResult<IReadOnlyList<OrderAttempt>> unknown = await _autoTrader.GetUnknownOrdersAsync();

        if (!unknown.IsSuccess)
        {
            Ui.Failure(unknown);
            return false;
        }

        Ui.Hint("Look up each order in your brokerage account by its client order ID or broker order ID.");
        Ui.Hint("If an order is still open there, cancel it at the brokerage first, then record its final state here.");

        foreach (OrderAttempt attempt in unknown.Value!)
        {
            Console.NewLine();
            Ui.Table(
                ["Time", "Side", "Requested", "Reference price", "Client order ID", "Broker order ID"],
                [[
                    Ui.LocalTime(attempt.AttemptedAtUtc),
                    attempt.Side.ToString(),
                    attempt.Side == OrderSide.Buy ? Ui.Usd(attempt.RequestedNotional) : Ui.Btc(attempt.RequestedQuantity),
                    Ui.Price(attempt.ReferencePrice),
                    attempt.Id.ToString("D"),
                    attempt.BrokerOrderId ?? "none"
                ]]);

            OrderReconciliation outcome;

            switch (Ui.Choose("What does the brokerage show for this order?", [FILLED, NOT_FILLED, LATER]))
            {
                case FILLED:
                    decimal? quantity = Ui.AskDecimal("BTC received or delivered (after any fee taken in BTC)", 0m);
                    decimal? price = Ui.AskDecimal("Average fill price (USD)", 0m);
                    decimal? fee = Ui.AskDecimal("Fee (USD)", 0m);
                    string filledAtText = Ui.Ask("Fill time in local time, as the brokerage shows it (for example 2026-11-01 14:05:30)");

                    if (quantity is null || price is null || fee is null
                        || !DateTimeOffset.TryParse(filledAtText, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out DateTimeOffset filledAt))
                    {
                        Ui.Error("That entry was not recognized; nothing was changed.");
                        return false;
                    }

                    outcome = OrderReconciliation.Fill(quantity.Value, price.Value, fee.Value, filledAt);
                    break;
                case NOT_FILLED:
                    outcome = OrderReconciliation.NotFilled;
                    break;
                default:
                    return false;
            }

            CapabilityResult<OrderAttempt> reconciled = await _autoTrader.ReconcileUnknownOrderAsync(attempt.Id, outcome);

            if (!reconciled.IsSuccess)
            {
                Ui.Failure(reconciled);
                return false;
            }

            Ui.Success($"Recorded the order as {reconciled.Value!.Status}.");
        }

        return true;
    }

    private async Task StopAsync()
    {
        Console.NewLine();
        await Ui.SpinAsync(StopAndReportAsync(), "Stopping after any in-flight order completes…");
        Ui.Success("Auto-trading stopped. Open positions were left as they are.");
        Ui.Pause();
    }

    private async Task<bool> ConfirmExitAsync()
    {
        if (!_autoTrader.IsRunning)
        {
            return true;
        }

        Console.NewLine();

        if (!Console.Confirm($"Auto-trading is running. Stop it and exit? ({Color.Green}y{Color.Default}/{Color.Red}n{Color.Default}) "))
        {
            return false;
        }

        await Ui.SpinAsync(StopAndReportAsync(), "Stopping…");
        return true;
    }

    private async Task<bool> StopAndReportAsync()
    {
        await _autoTrader.StopAsync();
        return true;
    }
}
