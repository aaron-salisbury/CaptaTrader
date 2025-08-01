using CaptaTrader.Presentation.CLI.Base;
using RunnethOverStudio.AppToolkit.Presentation.MVP;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static CaptaTrader.Presentation.CLI.Views.UtilitiesView;

namespace CaptaTrader.Presentation.CLI.Views;

internal sealed class UtilitiesView : View
{
    internal event EventHandler<UtilitiesViewEventArgs>? UtilityOptionSelected;

    internal enum UtilitiesOptions
    {
        [Display(Name = "Write Historical Trades to CSV")]
        ReportTrades,
        Return
    }

    public override void Show()
    {
        Clear();

        IEnumerable<UtilitiesOptions> options = Enum.GetValues(typeof(UtilitiesOptions)).Cast<UtilitiesOptions>();
        List<string> readableOptions = [.. options.Select(co => co.GetDisplayName())];

        string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Utilities")
                .PageSize(10)
                .AddChoices(readableOptions));

        UtilitiesOptions selectedOption = options.Where(mmo => string.Equals(mmo.GetDisplayName(), choice)).FirstOrDefault();

        UtilityOptionSelected?.Invoke(this, new UtilitiesViewEventArgs(selectedOption));
    }
}

internal class UtilitiesViewEventArgs(UtilitiesOptions utilityOption) : EventArgs
{
    internal UtilitiesOptions UtilityOption { get; set; } = utilityOption;
}
