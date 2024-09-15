using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using CaptaTrader.Business.Modules.UserAccess.DTOs;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using FluentValidation.Results;
using System.Threading.Tasks;
using static CaptaTrader.Presentation.CLI.Views.ConfigurationsView;

namespace CaptaTrader.Presentation.CLI.Presenters;

internal class ConfigurationsPresenter : Presenter
{
    private ConfigurationsView? _view;
    private ISessionValueProvider _session;
    private IUserService _userService;
    private EndUserDto? _currentUser;

    public ConfigurationsPresenter(ISessionValueProvider session, IUserService userService, Navigator navigator) : base(navigator)
    {
        _session = session;
        _userService = userService;
        _currentUser = _userService.GetEndUser(_session.GetUserID(), "UserConfig");
    }

    internal override void Display(View view)
    {
        _view = (ConfigurationsView)view;

        _view.ConfigurationOptionSelectedAsync += ConfigurationsView_ConfigurationOptionSelected;
        _view.ConfigurationEntered += ConfigurationsView_ConfigurationEntered;
        _view.ConfigurationSecretEntered += ConfigurationsView_ConfigurationSecretEntered;

        _view.Show();
    }

    internal override void Dismiss()
    {
        if (_view != null)
        {
            _view.ConfigurationOptionSelectedAsync -= ConfigurationsView_ConfigurationOptionSelected;
            _view.ConfigurationEntered -= ConfigurationsView_ConfigurationEntered;
            _view.ConfigurationEntered -= ConfigurationsView_ConfigurationSecretEntered;
        }
    }

    private async Task ConfigurationsView_ConfigurationOptionSelected(object? sender, ConfigViewEventArgs e)
    {
        switch (e.MenuOption)
        {
            case ConfigurationOptions.EnterEmail:
                _view?.PopupConfiguration(e.MenuOption);
                break;
            case ConfigurationOptions.EnterCoinApiAPIKey:
            case ConfigurationOptions.EnterBinanceAPIKey:
            case ConfigurationOptions.EnterBinanceSecretKey:
                _view?.PopupSecretConfiguration(e.MenuOption);
                break;
            case ConfigurationOptions.Save:
                await SaveAsync();
                break;
            case ConfigurationOptions.Return:
            default:
                Serilog.Log.Warning("Invalid configuration option selected.");
                Navigator.NavigateTo(typeof(MainMenuPresenter));
                break;
        }
    }

    private void ConfigurationsView_ConfigurationEntered(object? sender, ConfigViewEventArgs e)
    {
        if (_currentUser != null)
        {
            switch (e.MenuOption)
            {
                case ConfigurationOptions.EnterEmail:
                    _currentUser.UserConfig.Email = e.EnteredValue;
                    break;
            }
        }

        _view?.Show();
    }

    private void ConfigurationsView_ConfigurationSecretEntered(object? sender, ConfigViewEventArgs e)
    {
        if (_currentUser != null)
        {
            switch (e.MenuOption)
            {
                case ConfigurationOptions.EnterCoinApiAPIKey:
                    _currentUser.UserConfig.MarketDataAPIKey = e.EnteredValue;
                    break;
                case ConfigurationOptions.EnterBinanceAPIKey:
                    _currentUser.UserConfig.ExchangeAPIKey = e.EnteredValue;
                    break;
                case ConfigurationOptions.EnterBinanceSecretKey:
                    _currentUser.UserConfig.ExchangeSecretKey = e.EnteredValue;
                    break;
            }
        }

        _view?.Show();
    }

    private async Task SaveAsync()
    {
        ValidationResult result;

        if (_currentUser == null)
        {
            result = new ValidationResult([new ValidationFailure() { ErrorMessage = "No user found to update." }]);
        }
        else
        {
            result = await _userService.SaveEndUserAsync(_currentUser);
        }

        if (!result.IsValid)
        {
            _view?.PopupProcessFail("Save End User", result);
            _view?.Show();
        }

        Navigator.NavigateTo(typeof(MainMenuPresenter));
    }
}
