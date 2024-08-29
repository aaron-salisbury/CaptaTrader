using CaptaTrader.Presentation.CLI.Base;
using CaptaTrader.Presentation.CLI.Base.MVP;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CaptaTrader.Presentation.CLI.Views
{
    internal class ConfigurationsView : View
    {
        internal event EventHandler<ConfigurationOptions>? ConfigurationOptionSelected;
        internal event EventHandler<(ConfigurationOptions o, string v)>? ConfigurationEntered;
        internal event EventHandler<(ConfigurationOptions o, string v)>? ConfigurationSecretEntered;

        internal enum ConfigurationOptions
        {
            [Display(Name = "Enter E-Mail")]
            EnterEmail,
            [Display(Name = "Enter CoinApi API Key")]
            EnterCoinApiAPIKey,
            [Display(Name = "Enter Binance API Key")]
            EnterBinanceAPIKey,
            [Display(Name = "Enter Binance Secret Key")]
            EnterBinanceSecretKey,
            Save,
            Return
        }

        internal override void Show()
        {
            Application.ClearConsole();

            IEnumerable<ConfigurationOptions> options = Enum.GetValues(typeof(ConfigurationOptions)).Cast<ConfigurationOptions>();
            List<string> readableOptions = options.Select(co => co.GetDisplayName()).ToList();

            string readableChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Configurations")
                    .PageSize(10)
                    .AddChoices(readableOptions));

            ConfigurationOptions selectedOption = options.Where(co => string.Equals(co.GetDisplayName(), readableChoice)).FirstOrDefault();

            ConfigurationOptionSelected?.Invoke(this, selectedOption);
        }

        internal void RequestConfiguration(ConfigurationOptions configurationOption)
        {
            string optionValue = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, configurationOption.GetDisplayName()))).Trim();

            ConfigurationEntered?.Invoke(this, (configurationOption, optionValue));
        }

        internal void RequestSecretConfiguration(ConfigurationOptions configurationOption)
        {
            string optionValue = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, configurationOption.GetDisplayName())).Secret()).Trim();

            ConfigurationSecretEntered?.Invoke(this, (configurationOption, optionValue));
        }
    }
}
