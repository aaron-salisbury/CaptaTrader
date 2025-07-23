using Spectre.Console;
using System;
using System.Linq;

namespace CaptaTrader.Presentation.CLI.Base.MVP;

internal abstract class View
{
    internal const string PROMPT_FORMAT = "{0}: ";

    internal abstract void Show();

    internal void PopupProcessFail(string processName, FluentValidation.Results.ValidationResult validationResult)
    {
        if (validationResult.Errors.Count > 0)
        {
            string errorMessage = validationResult.Errors.Count == 1
                ? validationResult.Errors.First().ErrorMessage
                : Environment.NewLine + string.Join(Environment.NewLine, validationResult.Errors.Select(e => $" * {e.ErrorMessage}"));

            AnsiConsole.MarkupLineInterpolated($"[red]{processName} failed[/]: {errorMessage}");
        }
        else
        {
            AnsiConsole.MarkupLineInterpolated($"[red]{processName} failed[/].");
        }

        AnsiConsole.Write("Press any key to return...");

        Console.ReadKey();
    }

    internal static void ClearConsole()
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(Application.AppTitle);
        AnsiConsole.Write(Environment.NewLine);
    }
}
