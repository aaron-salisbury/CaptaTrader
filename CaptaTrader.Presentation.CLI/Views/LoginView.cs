using CaptaTrader.Presentation.CLI.Base;
using CaptaTrader.Presentation.CLI.Base.MVP;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;

namespace CaptaTrader.Presentation.CLI.Views
{
    internal class LoginView : View
    {
        internal event EventHandler<LoginMenuOptions>? LoginOptionSelected;
        internal event EventHandler<NetworkCredential>? LoginCredentialsEntered;
        internal event EventHandler<NetworkCredential>? RegisterNewCredentialsEntered;

        internal enum LoginMenuOptions
        {
            Login,
            [Display(Name = "Create New User")]
            CreateNewUser
        }

        internal override void Show()
        {
            Application.ClearConsole();

            IEnumerable<LoginMenuOptions> options = Enum.GetValues(typeof(LoginMenuOptions)).Cast<LoginMenuOptions>();
            List<string> readableOptions = options.Select(mmo => mmo.GetDisplayName()).ToList();

            string choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Login")
                    .PageSize(10)
                    .AddChoices(readableOptions));

            LoginMenuOptions selectedOption = options.Where(lo => string.Equals(lo.GetDisplayName(), choice)).FirstOrDefault();

            LoginOptionSelected?.Invoke(this, selectedOption);
        }

        internal void RequestLogin()
        {
            string userName = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, "Username"))).Trim();

            string pass = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, "Password")).Secret()).Trim();

            NetworkCredential credentials = new(userName, pass);

            LoginCredentialsEntered?.Invoke(this, credentials);
        }

        internal void RegisterNew()
        {
            string newUserName = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, "New Username"))).Trim();

            string newPass = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, "New Password")).Secret()).Trim();

            NetworkCredential credentials = new(newUserName, newPass);

            RegisterNewCredentialsEntered?.Invoke(this, credentials);
        }
    }
}
