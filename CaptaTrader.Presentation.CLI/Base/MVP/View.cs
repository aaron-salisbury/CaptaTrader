using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaptaTrader.Presentation.CLI.Base.MVP
{
    internal abstract class View
    {
        internal const string PROMPT_FORMAT = "{0}: ";

        internal virtual void Show() { throw new NotImplementedException(); }

        internal void PopupProcessFail(string processName, IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult>? results = null)
        {
            if (results != null)
            {
                string errorMessage = results.Count() == 1
                    ? results.First().ErrorMessage ?? string.Empty
                    : Environment.NewLine + string.Join(Environment.NewLine, results.Select(vr => $" * {vr.ErrorMessage}"));

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
}
