using CaptaTrader.Business.Services;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using Microsoft.Extensions.DependencyInjection;
using static CaptaTrader.Presentation.CLI.Views.ConfigurationsView;

namespace CaptaTrader.Presentation.CLI.Presenters
{
    internal class ConfigurationsPresenter : Presenter
    {
        private ConfigurationsView? _view;
        private IUserService _userService = Application.DomainServices!.GetRequiredService<IUserService>();

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
                    if (Application.SessionUser != null)
                    {
                        _userService.Save(Application.SessionUser);
                    }
                    Navigator?.NavigateTo(typeof(MainMenuPresenter));
                    break;
                case ConfigurationOptions.Return:
                default:
                    Serilog.Log.Warning("Invalid configuration option selected.");
                    Navigator?.NavigateTo(typeof(MainMenuPresenter));
                    break;
            }
        }

        private void ConfigurationsView_ConfigurationEntered(object? sender, ConfigViewEventArgs e)
        {
            if (Application.SessionUser != null)
            {
                switch (e.MenuOption)
                {
                    case ConfigurationOptions.EnterEmail:
                        Application.SessionUser.Email = e.EnteredValue;
                        break;
                }
            }

            Navigator?.NavigateTo(typeof(ConfigurationsPresenter));
        }

        private void ConfigurationsView_ConfigurationSecretEntered(object? sender, ConfigViewEventArgs e)
        {
            if (Application.SessionUser != null)
            {
                switch (e.MenuOption)
                {
                    case ConfigurationOptions.EnterCoinApiAPIKey:
                        Application.SessionUser.CoinAPIKey = e.EnteredValue;
                        break;
                    case ConfigurationOptions.EnterBinanceAPIKey:
                        Application.SessionUser.BinanceKey = e.EnteredValue;
                        break;
                    case ConfigurationOptions.EnterBinanceSecretKey:
                        Application.SessionUser.BinanceSecretKey = e.EnteredValue;
                        break;
                }
            }

            Navigator?.NavigateTo(typeof(ConfigurationsPresenter));
        }
    }
}
