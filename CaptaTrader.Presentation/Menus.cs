using CaptaTrader.Business.Base.Extensions;
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
    internal class Menus
    {
        private const string PROMPT_FORMAT = "{0}: ";

        private enum LoginOptions
        {
            Login,
            [Display(Name = "Create New User")]
            CreateNewUser
        }

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

        public bool SelectedLaunchApp { get; set; } = false;

        private Application _mainApp;

        internal Menus(Application mainApp)
        {
            _mainApp = mainApp;
        }

        internal bool PresentLogin()
        {
            IEnumerable<LoginOptions> options = Enum.GetValues(typeof(LoginOptions)).Cast<LoginOptions>();
            List<string> readableOptions = options.Select(mmo => mmo.GetDisplayName()).ToList();

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Login")
                    .PageSize(10)
                    .AddChoices(readableOptions));

            LoginOptions selectedOption = options.Where(lo => string.Equals(lo.GetDisplayName(), choice)).FirstOrDefault();

            switch (selectedOption)
            {
                case LoginOptions.Login:
                    AnsiConsole.Write(string.Format(PROMPT_FORMAT, "Username"));
                    string? username = Console.ReadLine();
                    string pass = ConsoleExtensions.RequestPassword(string.Format(PROMPT_FORMAT, "Password"));
                    return username != null && _mainApp.TradingManager.VerifyUser(username, pass.ToSecureString());
                case LoginOptions.CreateNewUser:
                    AnsiConsole.Write(string.Format(PROMPT_FORMAT, "New Username"));
                    string? newUsername = Console.ReadLine();
                    string newPass = ConsoleExtensions.RequestPassword(string.Format(PROMPT_FORMAT, "New Password"));
                    return Task.Run(() => _mainApp.TradingManager.CreateUserAsync(newUsername, newPass.ToSecureString())).Result;
                default:
                    return PresentLogin();
            }
        }

        internal async Task PresentMainMenuAsync()
        {
            _mainApp.ClearConsole();

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
                    SelectedLaunchApp = true;
                    //TODO: Bot options.
                    break;
                case MainMenuOptions.Exit:
                    break;
                default:
                    await PresentMainMenuAsync();
                    break;
            }
        }

        internal async Task PresentConfigurationsAsync()
        {
            _mainApp.ClearConsole();

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
                    if (_mainApp.TradingManager.User != null)
                    {
                        _mainApp.TradingManager.User.Email = Console.ReadLine();
                    }
                    await PresentConfigurationsAsync();
                    break;
                case ConfigurationOptions.EnterCoinApiAPIKey:
                    if (_mainApp.TradingManager.User != null)
                    {
                        _mainApp.TradingManager.User.CoinAPIKey = ConsoleExtensions
                            .RequestPassword(string.Format(PROMPT_FORMAT, readableChoice));
                    }
                    await PresentConfigurationsAsync();
                    break;
                case ConfigurationOptions.EnterBinanceAPIKey:
                    if (_mainApp.TradingManager.User != null)
                    {
                        _mainApp.TradingManager.User.BinanceKey = ConsoleExtensions
                            .RequestPassword(string.Format(PROMPT_FORMAT, readableChoice));
                    }
                    await PresentConfigurationsAsync();
                    break;
                case ConfigurationOptions.EnterBinanceSecretKey:
                    if (_mainApp.TradingManager.User != null)
                    {
                        _mainApp.TradingManager.User.BinanceSecretKey = ConsoleExtensions
                            .RequestPassword(string.Format(PROMPT_FORMAT, readableChoice));
                    }
                    await PresentConfigurationsAsync();
                    break;
                case ConfigurationOptions.Save:
                    _mainApp.TradingManager.Save();
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
