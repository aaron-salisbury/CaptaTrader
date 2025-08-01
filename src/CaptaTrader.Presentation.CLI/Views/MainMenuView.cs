using CaptaTrader.Presentation.CLI.Base;
using RunnethOverStudio.AppToolkit.Presentation.MVP;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static CaptaTrader.Presentation.CLI.Views.MainMenuView;

namespace CaptaTrader.Presentation.CLI.Views;

internal sealed class MainMenuView : View
{
    internal event EventHandler<MainMenuViewEventArgs>? MainMenuOptionSelected;

    internal enum MainMenuOptions
    {
        [Display(Name = "Launch Bot")]
        LaunchBot,
        [Display(Name = "Enter Configurations")]
        EnterConfigurations,
        Utilities,
        Exit
    }

    public override void Show()
    {
        Clear();

        IEnumerable<MainMenuOptions> options = Enum.GetValues(typeof(MainMenuOptions)).Cast<MainMenuOptions>();
        List<string> readableOptions = [.. options.Select(mmo => mmo.GetDisplayName())];

        string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Main Menu")
                .PageSize(10)
                .AddChoices(readableOptions));

        MainMenuOptions selectedOption = options.Where(mmo => string.Equals(mmo.GetDisplayName(), choice)).FirstOrDefault();

        MainMenuOptionSelected?.Invoke(this, new MainMenuViewEventArgs(selectedOption));
    }
}

internal class MainMenuViewEventArgs(MainMenuOptions mainMenuOption) : EventArgs
{
    internal MainMenuOptions MenuOption { get; set; } = mainMenuOption;
}
