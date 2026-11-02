using System;

namespace CaptaTrader.CLIApp;

/// <summary>
/// Command-line options. The app is interactive, so options only choose how it starts.
/// </summary>
internal sealed record AppOptions(bool IsDemo, bool ShowHelp, bool ShowVersion, string? Error)
{
    internal const string HELP_TEXT = """
        Usage: CaptaTrader [options]

          --demo       Replay a real, volatile historical day against a simulated account.
                       Uses a throwaway data folder; no login, no real accounts, no real tax files.
          --version    Show the version and exit.
          -h, --help   Show this help and exit.
        """;

    internal static AppOptions Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        bool isDemo = false;
        bool showHelp = false;
        bool showVersion = false;

        foreach (string arg in args)
        {
            switch (arg.ToLowerInvariant())
            {
                case "--demo":
                    isDemo = true;
                    break;
                case "--version":
                    showVersion = true;
                    break;
                case "-h" or "--help" or "-?" or "/?":
                    showHelp = true;
                    break;
                default:
                    return new AppOptions(false, true, false, $"Unknown option '{arg}'.");
            }
        }

        return new AppOptions(isDemo, showHelp, showVersion, null);
    }
}
