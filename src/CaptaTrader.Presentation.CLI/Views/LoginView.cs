using CaptaTrader.Presentation.CLI.Base;
using CaptaTrader.Presentation.CLI.Base.MVP;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using static CaptaTrader.Presentation.CLI.Views.LoginView;

namespace CaptaTrader.Presentation.CLI.Views;

internal sealed class LoginView : View
{
    internal event EventHandler<LoginViewEventArgs>? LoginOptionSelected;
    internal event EventHandler<LoginViewEventArgs>? LoginCredentialsEntered;
    internal event EventHandler<LoginViewEventArgs>? RegisterNewCredentialsEntered;

    internal enum LoginMenuOptions
    {
        Login,
        [Display(Name = "Create New User")]
        CreateNewUser,
        Exit
    }

    internal override void Show()
    {
        ClearConsole();

        IEnumerable<LoginMenuOptions> options = Enum.GetValues(typeof(LoginMenuOptions)).Cast<LoginMenuOptions>();
        List<string> readableOptions = options.Select(mmo => mmo.GetDisplayName()).ToList();

        string choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Login")
                .PageSize(10)
                .AddChoices(readableOptions));

        LoginMenuOptions selectedOption = options.Where(lo => string.Equals(lo.GetDisplayName(), choice)).FirstOrDefault();

        LoginOptionSelected?.Invoke(this, new LoginViewEventArgs() { MenuOption = selectedOption });
    }

    internal void PopupAuthenticate()
    {
        string userName = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, "Username"))).Trim();

        string pass = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, "Password")).Secret()).Trim();

        NetworkCredential credentials = new(userName, pass);

        LoginCredentialsEntered?.Invoke(this, new LoginViewEventArgs() { NetworkCredential = credentials });
    }

    internal void PopupRegister()
    {
        string newUserName = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, "New Username"))).Trim();

        string newPass = AnsiConsole.Prompt(new TextPrompt<string>(string.Format(PROMPT_FORMAT, "New Password")).Secret()).Trim();

        NetworkCredential credentials = new(newUserName, newPass);

        RegisterNewCredentialsEntered?.Invoke(this, new LoginViewEventArgs() { NetworkCredential = credentials });
    }
}

internal class LoginViewEventArgs : EventArgs
{
    internal LoginMenuOptions? MenuOption { get; set; }
    internal NetworkCredential? NetworkCredential { get; set; }
}
