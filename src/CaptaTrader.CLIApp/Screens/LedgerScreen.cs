using CaptaTrader.Business.Modules.AutoTrading;
using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.UserAccess;
using CaptaTrader.CLIApp.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaptaTrader.CLIApp.Screens;

internal sealed class LedgerScreen
{
    private const int ROWS = 25;

    private readonly ILedgerStore _ledger;
    private readonly AutoTrader _autoTrader;
    private readonly UserSession _session;

    public LedgerScreen(ILedgerStore ledger, AutoTrader autoTrader, UserSession session)
    {
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        _autoTrader = autoTrader ?? throw new ArgumentNullException(nameof(autoTrader));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    internal async Task RunAsync()
    {
        Ui.Screen($"Recent trades (last {ROWS} attempts)", _autoTrader.Status, _session.IsDemo);

        IReadOnlyList<OrderAttempt> attempts = await _ledger.GetRecentAttemptsAsync(ROWS);
        List<string[]> rows = [.. attempts.Select(attempt => new[]
        {
            Ui.LocalTime(attempt.AttemptedAtUtc),
            attempt.Side.ToString(),
            attempt.Status.ToString(),
            attempt.Side == OrderSide.Buy ? Ui.Usd(attempt.RequestedNotional) : Ui.Btc(attempt.RequestedQuantity),
            attempt.Status == OrderAttemptStatus.Filled ? Ui.Btc(attempt.FilledQuantity) : string.Empty,
            Ui.Price(attempt.AveragePrice ?? attempt.ReferencePrice),
            attempt.Fee is null ? string.Empty : Ui.Usd(attempt.Fee),
            Ui.Truncate(attempt.Status == OrderAttemptStatus.Filled ? attempt.Reason : attempt.Message ?? attempt.Reason, 36)
        })];

        Ui.Table(["Time", "Side", "Status", "Requested", "Filled", "Price", "Fee", "Note"], rows);
        Ui.Hint("The full ledger is in the app database; tax reports are generated from it.");
        Ui.Pause();
    }
}
