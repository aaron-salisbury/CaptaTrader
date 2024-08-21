using CaptaTrader.Business;
using CaptaTrader.Presentation.Base;
using CaptaTrader.Presentation.Base.Extensions;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation
{
    internal class Interactions
    {
        private const string PROMPT_FORMAT = "{0}: ";

        private enum MainMenuOptions
        {
            [Display(Name = "Enter Configurations")]
            EnterConfigurations,
            [Display(Name = "Launch Bot")]
            LaunchBot,
            Exit
        }

        private enum ConfigurationOptions
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

        private Manager _tradingManager;

        internal Interactions(Manager tradingManager)
        {
            _tradingManager = tradingManager;
        }

        internal async Task<bool> PresentMainMenuAsync()
        {
            Program.SetupConsole();

            bool launchBot = false;

            IEnumerable<MainMenuOptions> options = Enum.GetValues(typeof(MainMenuOptions)).Cast<MainMenuOptions>();
            List<string> readableOptions = options.Select(mmo => mmo.GetDisplayName()).ToList();

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Main Menu")
                    .PageSize(10)
                    .AddChoices(readableOptions));

            MainMenuOptions selectedOption = options.Where(mmo => string.Equals(mmo.GetDisplayName(), choice)).FirstOrDefault();

            switch (selectedOption)
            {
                case MainMenuOptions.EnterConfigurations:
                    await PresentConfigurationsAsync();
                    break;
                case MainMenuOptions.LaunchBot:
                    launchBot = true;
                    //TODO: Bot options.
                    break;
                case MainMenuOptions.Exit:
                    break;
                default:
                    await PresentMainMenuAsync();
                    break;
            }

            return launchBot;
        }

        internal async Task PresentConfigurationsAsync()
        {
            Program.SetupConsole();

            IEnumerable<ConfigurationOptions> options = Enum.GetValues(typeof(ConfigurationOptions)).Cast<ConfigurationOptions>();
            List<string> readableOptions = options.Select(co => co.GetDisplayName()).ToList();

            string readableChoice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Configurations")
                    .PageSize(10)
                    .AddChoices(readableOptions));

            ConfigurationOptions selectedOption = options.Where(co => string.Equals(co.GetDisplayName(), readableChoice)).FirstOrDefault();

            switch (selectedOption)
            {
                case ConfigurationOptions.EnterEmail:
                    AnsiConsole.Write(string.Format(PROMPT_FORMAT, readableChoice));
                    _tradingManager.AppData.Email = Console.ReadLine();
                    await PresentConfigurationsAsync();
                    break;
                case ConfigurationOptions.EnterCoinApiAPIKey:
                    await _tradingManager.AppData.SetCoinApiAPIKeyAsync(
                        ConsoleExtensions.RequestPassword(string.Format(PROMPT_FORMAT, readableChoice)), 
                        Program.EncryptPhrase);
                    await PresentConfigurationsAsync();
                    break;
                case ConfigurationOptions.EnterBinanceAPIKey:
                    await _tradingManager.AppData.SetBinanceAPIKeyAsync(
                        ConsoleExtensions.RequestPassword(string.Format(PROMPT_FORMAT, readableChoice)), 
                        Program.EncryptPhrase);
                    await PresentConfigurationsAsync();
                    break;
                case ConfigurationOptions.EnterBinanceSecretKey:
                    await _tradingManager.AppData.SetBinanceSecretAPIKeyAsync(
                        ConsoleExtensions.RequestPassword(string.Format(PROMPT_FORMAT, readableChoice)), 
                        Program.EncryptPhrase);
                    await PresentConfigurationsAsync();
                    break;
                case ConfigurationOptions.Save:
                    _tradingManager.Save();
                    await PresentConfigurationsAsync();
                    break;
                case ConfigurationOptions.Return:
                    await PresentMainMenuAsync();
                    break;
                default:
                    await PresentConfigurationsAsync();
                    break;
            }
        }
    }
}
