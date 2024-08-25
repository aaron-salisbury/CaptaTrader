using CaptaTrader.Business;
using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Presentation.CLI.Base;
using Spectre.Console;
using System.ComponentModel.DataAnnotations;

namespace CaptaTrader.Presentation.CLI
{
    internal class Menus
    {
        private static readonly string PROMPT_FORMAT = "{0}: ";

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

        internal static bool PresentLogin(AppManager appManager)
        {
            Application.ClearConsole();

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
                    string userName = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, "Username"))).Trim();
                    string pass = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, "Password")).Secret()).Trim();
                    return !string.IsNullOrEmpty(userName) && appManager.VerifyUser(userName, pass.ToSecureString());
                case LoginOptions.CreateNewUser:
                    string newUserName = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, "New Username"))).Trim();
                    string newPass = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, "New Password")).Secret()).Trim();
                    return Task.Run(() => appManager.CreateUserAsync(newUserName, newPass.ToSecureString())).Result;
            }

            return false;
        }

        internal static bool PresentMainMenuAsync(AppManager appManager)
        {
            Application.ClearConsole();
            bool selectedLaunchApp = false;

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
                    PresentConfigurationsAsync(appManager);
                    selectedLaunchApp = PresentMainMenuAsync(appManager);
                    break;
                case MainMenuOptions.LaunchBot:
                    selectedLaunchApp = true;
                    //TODO: Bot options?
                    break;
                case MainMenuOptions.Exit:
                    selectedLaunchApp = false;
                    break;
            }

            return selectedLaunchApp;
        }

        internal static void PresentConfigurationsAsync(AppManager appManager)
        {
            Application.ClearConsole();

            if (appManager.User == null)
            {
                Serilog.Log.Error("No user available to configure.");
                return; // Bubble up and close app. User shouldn't be able to get to configurations w/out first logging in.
            }

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
                    appManager.User.Email = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, readableChoice))).Trim();
                    PresentConfigurationsAsync(appManager);
                    break;
                case ConfigurationOptions.EnterCoinApiAPIKey:
                    appManager.User.CoinAPIKey = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, readableChoice)).Secret()).Trim();
                    PresentConfigurationsAsync(appManager);
                    break;
                case ConfigurationOptions.EnterBinanceAPIKey:
                    appManager.User.BinanceKey = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, readableChoice)).Secret()).Trim();
                    PresentConfigurationsAsync(appManager);
                    break;
                case ConfigurationOptions.EnterBinanceSecretKey:
                    appManager.User.BinanceSecretKey = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, readableChoice)).Secret()).Trim();
                    PresentConfigurationsAsync(appManager);
                    break;
                case ConfigurationOptions.Save:
                    appManager.Save();
                    PresentMainMenuAsync(appManager);
                    break;
                case ConfigurationOptions.Return:
                default:
                    return;
            }
        }
    }
}
