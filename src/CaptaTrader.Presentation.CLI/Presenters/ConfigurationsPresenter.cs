using CaptaTrader.Business.Modules.UserAccess.DTOs;
using CaptaTrader.Presentation.CLI.Views;
using RunnethOverStudio.AppToolkit.Core;
using RunnethOverStudio.AppToolkit.Presentation.MVP;
using System;
using static CaptaTrader.Presentation.CLI.Views.ConfigurationsView;

namespace CaptaTrader.Presentation.CLI.Presenters;

internal sealed class ConfigurationsPresenter(Navigator navigator) : Presenter(navigator)
{
    private UserConfigDto? _currentConfig;
    private ConfigurationsView? _view;

    public override void Display(View view)
    {
        _currentConfig = Application.TradingBot.UserAccess.GetSessionUserConfig();
        _view = (ConfigurationsView)view;

        _view.ConfigurationOptionSelected += ConfigurationsView_ConfigurationOptionSelected;
        _view.ConfigurationEntered += ConfigurationsView_ConfigurationEntered;
        _view.ConfigurationSecretEntered += ConfigurationsView_ConfigurationSecretEntered;

        _view.Show();
    }

    public override void Dismiss()
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
                Navigator.NavigateTo(typeof(MainMenuPresenter));
                break;
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
        ProcessResult result;

        if (_currentConfig == null)
        {
            result = new ProcessResult([new Exception("No user config found to update.")]);
        }
        else
        {
            result = Application.TradingBot.UserAccess.SaveSessionUserConfigAsync(_currentConfig).Result;
        }

        if (!result.IsValid)
        {
            _view?.ShowProcessFail("Save User Config", result);
            _view?.Show();
        }

        Navigator.NavigateTo(typeof(MainMenuPresenter));
    }
}
