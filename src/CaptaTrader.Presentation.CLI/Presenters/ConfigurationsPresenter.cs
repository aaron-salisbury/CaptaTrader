using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using CaptaTrader.Business.Modules.UserAccess.DTOs;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using FluentValidation.Results;
using static CaptaTrader.Presentation.CLI.Views.ConfigurationsView;

namespace CaptaTrader.Presentation.CLI.Presenters;

internal class ConfigurationsPresenter : Presenter
{
    private ConfigurationsView? _view;
    private IUserAccessService _userService;
    private UserConfigDto? _currentConfig;

    public ConfigurationsPresenter(IUserAccessService userService, Navigator navigator) : base(navigator)
    {
        _userService = userService;
        _currentConfig = _userService.GetSessionUserConfig();
    }

    internal override void Display(View view)
    {
        _view = (ConfigurationsView)view;

        _view.ConfigurationOptionSelected += ConfigurationsView_ConfigurationOptionSelected;
        _view.ConfigurationEntered += ConfigurationsView_ConfigurationEntered;
        _view.ConfigurationSecretEntered += ConfigurationsView_ConfigurationSecretEntered;

        _view.Show();
    }

    internal override void Dismiss()
    {
        if (_view != null)
        {
            _view.ConfigurationOptionSelected -= ConfigurationsView_ConfigurationOptionSelected;
            _view.ConfigurationEntered -= ConfigurationsView_ConfigurationEntered;
            _view.ConfigurationEntered -= ConfigurationsView_ConfigurationSecretEntered;
        }
    }

    private void ConfigurationsView_ConfigurationOptionSelected(object? sender, ConfigViewEventArgs e)
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
                Save();
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
        if (_currentConfig != null)
        {
            switch (e.MenuOption)
            {
                case ConfigurationOptions.EnterEmail:
                    _currentConfig.Email = e.EnteredValue;
                    break;
            }
        }

        _view?.Show();
    }

    private void ConfigurationsView_ConfigurationSecretEntered(object? sender, ConfigViewEventArgs e)
    {
        if (_currentConfig != null)
        {
            switch (e.MenuOption)
            {
                case ConfigurationOptions.EnterCoinApiAPIKey:
                    _currentConfig.MarketDataAPIKey = e.EnteredValue;
                    break;
                case ConfigurationOptions.EnterBinanceAPIKey:
                    _currentConfig.ExchangeAPIKey = e.EnteredValue;
                    break;
                case ConfigurationOptions.EnterBinanceSecretKey:
                    _currentConfig.ExchangeSecretKey = e.EnteredValue;
                    break;
            }
        }

        _view?.Show();
    }

    private void Save()
    {
        ValidationResult result;

        if (_currentConfig == null)
        {
            result = new ValidationResult([new ValidationFailure() { ErrorMessage = "No user config found to update." }]);
        }
        else
        {
            result = _userService.SaveSessionUserConfigAsync(_currentConfig).Result;
        }

        if (!result.IsValid)
        {
            _view?.PopupProcessFail("Save User Config", result);
            _view?.Show();
        }

        Navigator.NavigateTo(typeof(MainMenuPresenter));
    }
}
