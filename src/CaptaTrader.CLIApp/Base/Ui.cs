using CaptaTrader.Business.Modules.AutoTrading;
using CaptaTrader.Business.Modules.Capabilities;
using PrettyConsole;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptaTrader.CLIApp.Base;

/// <summary>
/// Console rendering shared by every screen: palette, banner, the trading status header, prompts, and
/// value formatting. Values are formatted with the invariant culture and an explicit "$" because every
/// amount in this app is USD, whatever the machine's locale.
/// </summary>
internal static class Ui
{
    /// <summary>Bitcoin orange from the 256-color palette.</summary>
    internal static readonly AnsiToken ACCENT = new("\e[38;5;208m");

    internal static readonly IReadOnlyList<string> SPINNER_FRAMES = Spinner.Patterns.Braille;

    private const string BANNER = """
        ╔═╗┌─┐┌─┐┌┬┐┌─┐  ╔╦╗┬─┐┌─┐┌┬┐┌─┐┬─┐
        ║  ├─┤├─┘ │ ├─┤   ║ ├┬┘├─┤ ││├┤ ├┬┘
        ╚═╝┴ ┴┴   ┴ ┴ ┴   ╩ ┴└─┴ ┴─┴┘└─┘┴└─
        """;

    /// <summary>
    /// True when both output pipes are a real terminal. Spinners and the live region move the cursor,
    /// which redirected output (a file or pipe) cannot do, so those fall back to plain lines.
    /// </summary>
    internal static bool IsInteractive => !System.Console.IsOutputRedirected && !System.Console.IsErrorRedirected;

    /// <summary>Clears the screen when there is one; redirected output has nothing to clear.</summary>
    internal static void Clear()
    {
        if (IsInteractive)
        {
            System.Console.Clear();
        }
    }

    internal static void Banner()
    {
        Console.NewLine();

        foreach (string line in BANNER.Split('\n'))
        {
            Console.WriteLineInterpolated($"  {ACCENT}{line.TrimEnd('\r')}");
        }

        Console.WriteLineInterpolated($"  {Color.DarkGray}Bitcoin volatility bot · v{DependencyInjection.Version} · as-is, for education, not financial advice");
        Console.NewLine();
    }

    /// <summary>
    /// Clears the screen and draws the status header. The header is the in-screen half of the
    /// "bot is active" cue; <see cref="WindowTitle"/> is the half that persists while a prompt blocks.
    /// </summary>
    internal static void Screen(string title, TradingStatus status, bool isDemo)
    {
        Ui.Clear();
        StatusHeader(status, isDemo);
        Console.WriteLineInterpolated($"{Markup.Bold}{ACCENT}{title}{Markup.Reset}");
        Console.WriteLineInterpolated($"{Color.DarkGray}{new string('─', Math.Max(title.Length, 24))}");
    }

    internal static void StatusHeader(TradingStatus status, bool isDemo)
    {
        if (isDemo)
        {
            Console.WriteInterpolated($"{Color.Black}{Color.YellowBackground} DEMO {Color.Default} ");
        }

        if (status.IsRunning)
        {
            Console.WriteInterpolated($"{Color.Black}{Color.GreenBackground} ● AUTO-TRADING ACTIVE {Color.Default} ");
            Console.WriteLineInterpolated($"{Color.DarkGray}{status.BrokerageName} · {status.MarketDataName} · BTC {Price(status.LastTick?.Price)}");
        }
        else
        {
            Console.WriteLineInterpolated($"{Color.DarkGray}○ Auto-trading stopped");
        }

        Console.NewLine();
    }

    internal static void Success(string message)
    {
        Console.WriteLineInterpolated($"{Color.Green}✔ {message}");
    }

    internal static void Warning(string message)
    {
        Console.WriteLineInterpolated($"{Color.Yellow}⚠ {message}");
    }

    internal static void Error(string message)
    {
        Console.WriteLineInterpolated($"{Color.Red}✖ {message}");
    }

    internal static void Hint(string message)
    {
        Console.WriteLineInterpolated($"{Color.DarkGray}{message}");
    }

    internal static void Failure<T>(CapabilityResult<T> result)
    {
        if (result.Status is CapabilityStatus.NotConfigured or CapabilityStatus.InvalidRequest or CapabilityStatus.Busy)
        {
            Warning(result.Message ?? "Not available.");
        }
        else
        {
            Error(result.Message ?? "Something went wrong.");
        }
    }

    internal static void Pause()
    {
        Console.NewLine();

        if (System.Console.IsInputRedirected)
        {
            // Without a keyboard there is no key to wait for; consume one line instead.
            Hint("Press Enter to continue…");
            System.Console.ReadLine();
            return;
        }

        Console.RequestAnyInput($"{Color.DarkGray}Press any key to continue…");
    }

    /// <summary>Numbered menu built on PrettyConsole's selection. Returns null when the entry was invalid.</summary>
    internal static string? Choose(string title, IList<string> choices)
    {
        string choice = Console.Selection(choices, $"{Markup.Bold}{title}{Markup.Reset}");

        return choice.Length == 0 ? null : choice;
    }

    internal static string Ask(string prompt)
    {
        return Console.ReadLine($"{ACCENT}›{Color.Default} {prompt}: ").Trim();
    }

    internal static decimal? AskDecimal(string prompt, decimal current)
    {
        string input = Console.ReadLine($"{ACCENT}›{Color.Default} {prompt} {Color.DarkGray}[{current.ToString(CultureInfo.InvariantCulture)}]{Color.Default}: ").Trim();

        if (input.Length == 0)
        {
            return current;
        }

        return decimal.TryParse(input.TrimStart('$').TrimEnd('%'), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) ? value : null;
    }

    /// <summary>
    /// Reads a line without echoing it, showing a bullet per character. Falls back to a plain read when
    /// input is redirected (no keyboard to intercept).
    /// </summary>
    internal static string AskSecret(string prompt)
    {
        Console.WriteInterpolated($"{ACCENT}›{Color.Default} {prompt}: ");

        if (System.Console.IsInputRedirected)
        {
            return System.Console.ReadLine() ?? string.Empty;
        }

        StringBuilder builder = new();

        while (true)
        {
            ConsoleKeyInfo key = System.Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.NewLine();
                return builder.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (builder.Length > 0)
                {
                    builder.Length--;
                    System.Console.Write("\b \b");
                }

                continue;
            }

            if (!char.IsControl(key.KeyChar))
            {
                builder.Append(key.KeyChar);
                System.Console.Write('•');
            }
        }
    }

    /// <summary>Runs a task under PrettyConsole's spinner, then clears the spinner line.</summary>
    internal static async Task<T> SpinAsync<T>(Task<T> task, string header)
    {
        if (!IsInteractive)
        {
            Hint(header);
            return await task;
        }

        Spinner spinner = new()
        {
            Pattern = Spinner.Patterns.Braille,
            ForegroundColor = ACCENT,
            DisplayElapsedTime = false,
            UpdateRate = 80
        };

        try
        {
            return await spinner.RunAsync(task, header);
        }
        finally
        {
            Console.ClearNextLines(1, OutputPipe.Error);
        }
    }

    internal static string Usd(decimal? value)
    {
        if (value is null)
        {
            return "—";
        }

        string formatted = Math.Abs(value.Value).ToString("#,##0.00", CultureInfo.InvariantCulture);

        return value.Value < 0m ? "-$" + formatted : "$" + formatted;
    }

    internal static string SignedUsd(decimal value)
    {
        return value >= 0m ? "+" + Usd(value) : Usd(value);
    }

    internal static string Price(decimal? value)
    {
        return Usd(value);
    }

    internal static string Btc(decimal? value)
    {
        return value is null ? "—" : value.Value.ToString("0.00000000", CultureInfo.InvariantCulture) + " BTC";
    }

    internal static string Percent(decimal value)
    {
        return value.ToString("0.00%", CultureInfo.InvariantCulture);
    }

    internal static string LocalTime(DateTimeOffset instant)
    {
        DateTimeOffset local = instant.ToLocalTime();

        return local.Date == DateTimeOffset.Now.Date
            ? local.ToString("HH:mm:ss", CultureInfo.InvariantCulture)
            : local.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    internal static string Truncate(string? value, int length)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= length ? value : value[..(length - 1)] + "…";
    }

    /// <summary>
    /// Draws a table with PrettyConsole, padding each column to at least its header width (the
    /// library sizes columns from values only).
    /// </summary>
    internal static void Table(string[] headers, IReadOnlyList<string[]> rows)
    {
        if (rows.Count == 0)
        {
            Hint("(none)");
            return;
        }

        string[][] columns = [.. headers.Select((header, index) => rows.Select(row => row[index].PadRight(header.Length)).ToArray())];
        Console.Table(headers, columns);
    }
}
