using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Models;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static CaptaTrader.Presentation.CLI.Views.ConfigurationsView;

namespace CaptaTrader.Presentation.CLI.Presenters;

internal class ConfigurationsPresenter : Presenter
{
    private ConfigurationsView? _view;
    private ISessionValueProvider _session;
    private IUserService _userService;

    public ConfigurationsPresenter(ISessionValueProvider session, IUserService userService, Navigator navigator) : base(navigator)
    {
        _session = session;
        _userService = userService;
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
        if (_userService.GetEndUser(_session.GetUserID()) is EndUser user)
        {
            switch (e.MenuOption)
            {
                case ConfigurationOptions.EnterEmail:
                    user.Email = e.EnteredValue;
                    break;
            }
        }

        Navigator.NavigateTo(typeof(ConfigurationsPresenter));
    }

    private void ConfigurationsView_ConfigurationSecretEntered(object? sender, ConfigViewEventArgs e)
    {
        if (_userService.GetEndUser(_session.GetUserID()) is EndUser user)
        {
            switch (e.MenuOption)
            {
                case ConfigurationOptions.EnterCoinApiAPIKey:
                    user.MarketDataAPIKey = e.EnteredValue;
                    break;
                case ConfigurationOptions.EnterBinanceAPIKey:
                    user.ExchangeAPIKey = e.EnteredValue;
                    break;
                case ConfigurationOptions.EnterBinanceSecretKey:
                    user.ExchangeSecretKey = e.EnteredValue;
                    break;
            }
        }

        Navigator.NavigateTo(typeof(ConfigurationsPresenter));
    }

    private void Save()
    {
        IEnumerable<ValidationResult> results;
        EndUser? user = _userService.GetEndUser(_session.GetUserID());

        if (user == null)
        {
            results = [new ValidationResult("No user found to update.")];
        }
        else
        {
            results = _userService.SaveEndUser(user);
        }

        if (results.Any())
        {
            _view?.PopupProcessFail(nameof(IUserService.SaveEndUser), results);
        }

        Navigator.NavigateTo(typeof(MainMenuPresenter));
    }
}
