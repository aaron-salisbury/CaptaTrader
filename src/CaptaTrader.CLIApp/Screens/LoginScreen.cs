using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.UserAccess;
using CaptaTrader.CLIApp.Base;
using PrettyConsole;
using System;
using System.Threading.Tasks;

namespace CaptaTrader.CLIApp.Screens;

/// <summary>
/// First run offers only "create login"; every later run offers only "log in", because the tool is
/// one-to-one with its user.
/// </summary>
internal sealed class LoginScreen
{
    private const int MAXIMUM_ATTEMPTS = 5;
    private const string CREATE = "Create new login";
    private const string EXIT = "Exit";

    private readonly ManageLogin _manageLogin;

    public LoginScreen(ManageLogin manageLogin)
    {
        _manageLogin = manageLogin ?? throw new ArgumentNullException(nameof(manageLogin));
    }

    /// <returns><see langword="true"/> when the user is signed in.</returns>
    internal async Task<bool> RunAsync()
    {
        Ui.Clear();
        Ui.Banner();

        if (await _manageLogin.LoginExistsAsync())
        {
            return await LogInAsync();
        }

        return await CreateLoginAsync();
    }

    private async Task<bool> CreateLoginAsync()
    {
        Console.WriteLineInterpolated($"Welcome. No login exists on this computer yet.");
        Ui.Hint("CaptaTrader is single-user: this login is the only one, and its password cannot be recovered.");
        Console.NewLine();

        if (Ui.Choose("What would you like to do?", [CREATE, EXIT]) != CREATE)
        {
            return false;
        }

        while (true)
        {
            Console.NewLine();
            string userName = Ui.Ask("User name");
            string password = Ui.AskSecret($"Password ({ManageLogin.MINIMUM_PASSWORD_LENGTH}+ characters)");
            string confirmation = Ui.AskSecret("Confirm password");

            CapabilityResult<string> result = await Ui.SpinAsync(
                _manageLogin.CreateLoginAsync(userName, password, confirmation),
                "Securing your login…");

            if (result.IsSuccess)
            {
                Ui.Success($"Login created. Welcome, {result.Value}.");
                return true;
            }

            Ui.Failure(result);

            if (result.Status != CapabilityStatus.InvalidRequest || !Console.Confirm($"Try again? ({Color.Green}y{Color.Default}/{Color.Red}n{Color.Default}) "))
            {
                return false;
            }
        }
    }

    private async Task<bool> LogInAsync()
    {
        Console.WriteLineInterpolated($"Log in to continue.");

        for (int attempt = 1; attempt <= MAXIMUM_ATTEMPTS; attempt++)
        {
            Console.NewLine();
            string userName = Ui.Ask("User name");
            string password = Ui.AskSecret("Password");

            CapabilityResult<string> result = await Ui.SpinAsync(_manageLogin.LogInAsync(userName, password), "Verifying…");

            if (result.IsSuccess)
            {
                Ui.Success($"Welcome back, {result.Value}.");
                return true;
            }

            Ui.Failure(result);

            if (attempt < MAXIMUM_ATTEMPTS)
            {
                // Growing delay slows guessing without punishing a typo.
                await Task.Delay(TimeSpan.FromSeconds(attempt));
            }
        }

        Ui.Error("Too many failed attempts.");
        return false;
    }
}
