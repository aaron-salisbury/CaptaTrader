using CaptaTrader.Business.DTOs;
using CaptaTrader.Presentation.Base;
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

        private Action _saveAction;
        private Action<string> _launchBotAction;

        internal Interactions(Action saveAction, Action<string> launchBotAction)
        {
            _saveAction = saveAction;
            _launchBotAction = launchBotAction;
        }

        internal async Task PresentMainMenuAsync(AppData appData, string passPhrase)
        {
            // https://spectreconsole.net/prompts/selection

            List<string> mainMenuOptions = Enum.GetValues(typeof(MainMenuOptions))
                .Cast<MainMenuOptions>()
                .Select(mmo => mmo.GetAttribute<DisplayAttribute>()?.Name ?? mmo.ToString())
                .ToList();

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Main Menu")
                    .PageSize(10)
                    .AddChoices(mainMenuOptions));

            switch (choice) //TODO: Use the enum that matches the display attribute.
            {
                case "Enter Configurations":
                    await PresentConfigurationsAsync(appData, passPhrase);
                    break;
                case "Launch Bot":
                    _launchBotAction.Invoke(passPhrase);
                    //TODO: Bot options.
                    break;
                case "Exit":
                    return;
                default:
                    await PresentMainMenuAsync(appData, passPhrase);
                    break;
            }
        }

        internal async Task PresentConfigurationsAsync(AppData appData, string passPhrase)
        {
            List<string> configurationOptions = Enum.GetValues(typeof(ConfigurationOptions))
                .Cast<ConfigurationOptions>()
                .Select(mmo => mmo.GetAttribute<DisplayAttribute>()?.Name ?? mmo.ToString())
                .ToList();

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Configurations")
                    .PageSize(10)
                    .AddChoices(configurationOptions));

            switch (choice)
            {
                case "Enter E-Mail":
                    Console.Write("Enter E-Mail: ");
                    appData.Email = Console.ReadLine();
                    await PresentConfigurationsAsync(appData, passPhrase);
                    break;
                case "Enter CoinApi API Key":
                    Console.Write("Enter Alpha Vantage API Key: ");
                    string? coinApiAPIKey = Console.ReadLine();
                    await appData.SetCoinApiAPIKeyAsync(coinApiAPIKey ?? string.Empty, passPhrase);
                    await PresentConfigurationsAsync(appData, passPhrase);
                    break;
                case "Enter Binance API Key":
                    Console.Write("Enter Binance API Key: ");
                    string? binanceAPIKey = Console.ReadLine();
                    await appData.SetBinanceAPIKeyAsync(binanceAPIKey ?? string.Empty, passPhrase);
                    await PresentConfigurationsAsync(appData, passPhrase);
                    break;
                case "Enter Binance Secret Key":
                    Console.Write("Enter Binance Secret Key: ");
                    string? binanceSecretAPIKey = Console.ReadLine(); //TODO: This should not show in console.
                    await appData.SetBinanceSecretAPIKeyAsync(binanceSecretAPIKey ?? string.Empty, passPhrase);
                    await PresentConfigurationsAsync(appData, passPhrase);
                    break;
                case "Save":
                    _saveAction.Invoke();
                    await PresentConfigurationsAsync(appData, passPhrase);
                    break;
                case "Return":
                    await PresentMainMenuAsync(appData, passPhrase);
                    break;
                default:
                    await PresentConfigurationsAsync(appData, passPhrase);
                    break;
            }
        }
    }
}
