using CaptaTrader.Business.Modules.AutoTrading;
using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.UserAccess;
using CaptaTrader.CLIApp.Base;
using PrettyConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaptaTrader.CLIApp.Screens;

/// <summary>
/// Live view of a trading session. Order attempts and outcomes stream as durable lines while a pinned
/// region at the bottom animates a spinner with the current price, position, and limits.
/// </summary>
internal sealed class TradingMonitorScreen
{
    private static readonly TimeSpan FRAME_INTERVAL = TimeSpan.FromMilliseconds(100);
    private const int RECENT_ATTEMPTS = 5;

    private readonly AutoTrader _autoTrader;
    private readonly ILedgerStore _ledger;
    private readonly UserSession _session;

    public TradingMonitorScreen(AutoTrader autoTrader, ILedgerStore ledger, UserSession session)
    {
        _autoTrader = autoTrader ?? throw new ArgumentNullException(nameof(autoTrader));
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    internal async Task RunAsync()
    {
        Ui.Screen("Live trading", _autoTrader.Status, _session.IsDemo);
        await ShowCatchUpAsync();

        int frame = 0;
        bool stopRequested = false;

        using (LiveConsoleRegion region = new(OutputPipe.Out))
        {
            while (true)
            {
                while (_autoTrader.Events.TryRead(out TradingEvent? tradingEvent))
                {
                    Write(region, tradingEvent);
                }

                TradingStatus status = _autoTrader.Status;

                if (!status.IsRunning)
                {
                    break;
                }

                if (!Ui.IsInteractive)
                {
                    // Redirected output gets the durable lines only; there is no cursor to pin a region with.
                }
                else if (stopRequested)
                {
                    region.Render($"{Color.Yellow}{Ui.SPINNER_FRAMES[frame]} Stopping after any in-flight order completes…");
                }
                else
                {
                    Render(region, status, Ui.SPINNER_FRAMES[frame]);
                }

                frame = (frame + 1) % Ui.SPINNER_FRAMES.Count;

                ConsoleKey? key = ReadKey();

                if (key is ConsoleKey.S && !stopRequested)
                {
                    stopRequested = true;
                    _ = _autoTrader.StopAsync();
                }
                else if (key is ConsoleKey.B or ConsoleKey.Escape && !stopRequested)
                {
                    return;
                }

                await Task.Delay(FRAME_INTERVAL);
            }

            // Drain anything published while stopping, such as the final order and the stop reason.
            while (_autoTrader.Events.TryRead(out TradingEvent? tradingEvent))
            {
                Write(region, tradingEvent);
            }
        }

        WriteSummary(_autoTrader.Status);
        Ui.Pause();
    }

    /// <summary>
    /// Events published while the user was in a menu are already on the ledger, so show the latest
    /// attempts from there instead of replaying a long backlog.
    /// </summary>
    private async Task ShowCatchUpAsync()
    {
        int skipped = 0;

        while (_autoTrader.Events.TryPeek(out TradingEvent? pending) && pending is not SessionStarted)
        {
            _autoTrader.Events.TryRead(out _);
            skipped++;
        }

        IReadOnlyList<OrderAttempt> recent = await _ledger.GetRecentAttemptsAsync(RECENT_ATTEMPTS);

        if (skipped > 0 && recent.Count > 0)
        {
            Ui.Hint($"Most recent attempts (full history under Recent trades):");

            foreach (OrderAttempt attempt in recent.Reverse())
            {
                Ui.Hint($"  {Ui.LocalTime(attempt.AttemptedAtUtc)}  {attempt.Side,-4} {attempt.Status,-8} {Describe(attempt)}");
            }

            Console.NewLine();
        }

        Ui.Hint("[S] stop trading   [B] back to the menu (trading continues)");
        Console.NewLine();
    }

    private static void Render(LiveConsoleRegion region, TradingStatus status, string frame)
    {
        decimal? price = status.LastTick?.Price;
        decimal change = price is not null && status.SessionOpenPrice is > 0m ? (price.Value - status.SessionOpenPrice.Value) / status.SessionOpenPrice.Value : 0m;
        AnsiToken changeColor = change >= 0m ? Color.Green : Color.Red;
        string arrow = change >= 0m ? "▲" : "▼";
        decimal unrealized = status.UnrealizedGain ?? 0m;
        AnsiToken unrealizedColor = unrealized >= 0m ? Color.Green : Color.Red;
        AnsiToken realizedColor = status.RealizedGain >= 0m ? Color.Green : Color.Red;
        string activity = status.InFlight is null
            ? "AUTO-TRADING ACTIVE"
            : $"SUBMITTING {status.InFlight.Side.ToString().ToUpperInvariant()} {DescribeRequest(status.InFlight)}";
        string tickTime = status.LastTick is null ? "waiting for prices" : Ui.LocalTime(status.LastTick.Time);

        region.Render($"""
            {Color.Green}{frame} {Markup.Bold}{activity}{Markup.Reset}  {Color.DarkGray}{tickTime}{Color.Default}
              BTC {Markup.Bold}{Ui.Price(price)}{Markup.Reset} {changeColor}{arrow}{Ui.Percent(Math.Abs(change))}{Color.Default}   held {Ui.Btc(status.HeldQuantity)} {unrealizedColor}({Ui.SignedUsd(unrealized)}){Color.Default}   cash {Ui.Usd(status.Account?.CashUsd)}
              {Color.DarkGray}orders/hour {status.OrdersInLastHour}/{status.MaxOrdersPerHour} · fills {status.Fills} · realized {realizedColor}{Ui.SignedUsd(status.RealizedGain)}{Color.DarkGray} · {Ui.Truncate(status.LastDecision, 28)}
            """);
    }

    private static void Write(LiveConsoleRegion region, TradingEvent tradingEvent)
    {
        string time = Ui.LocalTime(tradingEvent.At);

        switch (tradingEvent)
        {
            case SessionStarted started:
                region.WriteLine($"{Ui.ACCENT}▶ Session started{Color.Default} · {started.BrokerageName} · {started.MarketDataName} · {started.StrategyName}{(started.IsSimulated ? " · simulated money" : string.Empty)}");
                break;
            case OrderAttempted attempted:
                region.WriteLine($"{Color.DarkGray}{time}{Color.Default} {Color.Cyan}⇢ {attempted.Attempt.Side.ToString().ToUpperInvariant(),-4}{Color.Default} {DescribeRequest(attempted.Attempt)} at ~{Ui.Price(attempted.Attempt.ReferencePrice)}  {Color.DarkGray}{Ui.Truncate(attempted.Attempt.Reason, 48)}");
                break;
            case OrderCompleted { Attempt.Status: OrderAttemptStatus.Filled } filled:
                WriteFill(region, time, filled);
                break;
            case OrderCompleted { Attempt.Status: OrderAttemptStatus.Rejected } rejected:
                region.WriteLine($"{Color.DarkGray}{time}{Color.Default} {Color.Red}✖ REJECTED{Color.Default} {Ui.Truncate(rejected.Attempt.Message, 70)}");
                break;
            case OrderCompleted other:
                region.WriteLine($"{Color.DarkGray}{time}{Color.Default} {Color.Magenta}⚠ {other.Attempt.Status.ToString().ToUpperInvariant()}{Color.Default} {Ui.Truncate(other.Attempt.Message, 70)}");
                break;
            case OrderBlocked blocked:
                region.WriteLine($"{Color.DarkGray}{time} ⊘ {blocked.Decision.Action} blocked: {blocked.Reason}");
                break;
            case TradingNotice { Level: NoticeLevel.Error } error:
                region.WriteLine($"{Color.DarkGray}{time}{Color.Default} {Color.Red}✖ {error.Message}");
                break;
            case TradingNotice notice:
                region.WriteLine($"{Color.DarkGray}{time}{Color.Default} {Color.Yellow}⚠ {notice.Message}");
                break;
            case SessionStopped stopped:
                region.WriteLine($"{Ui.ACCENT}■ Session stopped{Color.Default} · {stopped.Reason}");
                break;
        }
    }

    private static void WriteFill(LiveConsoleRegion region, string time, OrderCompleted filled)
    {
        OrderAttempt attempt = filled.Attempt;
        string verb = attempt.Side == OrderSide.Buy ? "BOUGHT" : "SOLD";
        string fee = Ui.Usd(attempt.Fee) + (attempt.FeeIsEstimated ? " est." : string.Empty);

        if (filled.RealizedGain is decimal gain)
        {
            AnsiToken gainColor = gain >= 0m ? Color.Green : Color.Red;
            region.WriteLine($"{Color.DarkGray}{time}{Color.Default} {Color.Green}✔ {verb,-6}{Color.Default} {Ui.Btc(attempt.FilledQuantity)} @ {Ui.Price(attempt.AveragePrice)} · fee {fee} · realized {gainColor}{Ui.SignedUsd(gain)}");
        }
        else
        {
            region.WriteLine($"{Color.DarkGray}{time}{Color.Default} {Color.Green}✔ {verb,-6}{Color.Default} {Ui.Btc(attempt.FilledQuantity)} @ {Ui.Price(attempt.AveragePrice)} · fee {fee}");
        }
    }

    private static void WriteSummary(TradingStatus status)
    {
        Console.NewLine();
        AnsiToken realizedColor = status.RealizedGain >= 0m ? Color.Green : Color.Red;
        Console.WriteLineInterpolated($"{Markup.Bold}Session summary{Markup.Reset}: {status.Attempts} attempts, {status.Fills} fills, realized {realizedColor}{Ui.SignedUsd(status.RealizedGain)}{Color.Default} (before tax).");
        Console.WriteLineInterpolated($"Still held by the app: {Ui.Btc(status.HeldQuantity)}, cost basis {Ui.Usd(status.HeldCostBasis)}. Stopping never sells open positions.");
    }

    private static string DescribeRequest(OrderAttempt attempt)
    {
        return attempt.Side == OrderSide.Buy ? Ui.Usd(attempt.RequestedNotional) : Ui.Btc(attempt.RequestedQuantity);
    }

    private static string Describe(OrderAttempt attempt)
    {
        return attempt.Status == OrderAttemptStatus.Filled
            ? $"{Ui.Btc(attempt.FilledQuantity)} @ {Ui.Price(attempt.AveragePrice)}"
            : DescribeRequest(attempt);
    }

    private static ConsoleKey? ReadKey()
    {
        if (System.Console.IsInputRedirected || !System.Console.KeyAvailable)
        {
            return null;
        }

        return System.Console.ReadKey(intercept: true).Key;
    }
}
