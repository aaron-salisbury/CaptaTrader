using CaptaTrader.Presentation.CLI.Base.MVP;
using Spectre.Console;
using System;

namespace CaptaTrader.Presentation.CLI.Views
{
    internal class BotView : View
    {
        internal event EventHandler? ShutdownRequested;

        internal override void Show()
        {
            Application.ClearConsole();

            AnsiConsole.MarkupLine("Bot is [lime]active[/], press any key to shutdown..." + Environment.NewLine);
            Console.ReadKey();

            ShutdownRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
