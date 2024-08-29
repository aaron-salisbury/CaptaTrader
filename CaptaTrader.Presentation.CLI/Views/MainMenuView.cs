using CaptaTrader.Presentation.CLI.Base;
using CaptaTrader.Presentation.CLI.Base.MVP;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CaptaTrader.Presentation.CLI.Views
{
    internal class MainMenuView : View
    {
        internal event EventHandler<MainMenuOptions>? MainMenuOptionSelected;

        internal enum MainMenuOptions
        {
            [Display(Name = "Enter Configurations")]
            EnterConfigurations,
            [Display(Name = "Launch Bot")]
            LaunchBot,
            Exit
        }

        internal override void Show()
        {
            Application.ClearConsole();

            IEnumerable<MainMenuOptions> options = Enum.GetValues(typeof(MainMenuOptions)).Cast<MainMenuOptions>();
            List<string> readableOptions = options.Select(mmo => mmo.GetDisplayName()).ToList();

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Main Menu")
                    .PageSize(10)
                    .AddChoices(readableOptions));

            MainMenuOptions selectedOption = options.Where(mmo => string.Equals(mmo.GetDisplayName(), choice)).FirstOrDefault();

            MainMenuOptionSelected?.Invoke(this, selectedOption);
        }
    }
}
