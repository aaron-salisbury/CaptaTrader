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
        private IUserService? _userService = Application.DomainServices?.GetRequiredService<IUserService>();

        internal override void Display()
        {
            _view = ViewLocator.GetView<ConfigurationsView>(this);

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

        private void ConfigurationsView_ConfigurationOptionSelected(object? sender, ConfigurationOptions selectedOption)
        {
            switch (selectedOption)
            {
                case ConfigurationOptions.EnterEmail:
                    _view?.RequestConfiguration(selectedOption);
                    break;
                case ConfigurationOptions.EnterCoinApiAPIKey:
                case ConfigurationOptions.EnterBinanceAPIKey:
                case ConfigurationOptions.EnterBinanceSecretKey:
                    _view?.RequestSecretConfiguration(selectedOption);
                    break;
                case ConfigurationOptions.Save:
                    if (_userService != null && Application.SessionUser != null)
                    {
                        _userService.Save(Application.SessionUser);
                    }
                    Navigator?.NavigateTo(typeof(MainMenuPresenter));
                    break;
                case ConfigurationOptions.Return:
                default:
                    Navigator?.NavigateTo(typeof(MainMenuPresenter));
                    break;
            }
        }

        private void ConfigurationsView_ConfigurationEntered(object? sender, (ConfigurationOptions option, string newValue) optionAndItsNewValue)
        {
            if (Application.SessionUser != null)
            {
                switch (optionAndItsNewValue.option)
                {
                    case ConfigurationOptions.EnterEmail:
                        Application.SessionUser.Email = optionAndItsNewValue.newValue;
                        break;
                }
            }

            Navigator?.NavigateTo(typeof(ConfigurationsPresenter));
        }

        private void ConfigurationsView_ConfigurationSecretEntered(object? sender, (ConfigurationOptions option, string newValue) optionAndItsNewValue)
        {
            if (Application.SessionUser != null)
            {
                switch (optionAndItsNewValue.option)
                {
                    case ConfigurationOptions.EnterCoinApiAPIKey:
                        Application.SessionUser.CoinAPIKey = optionAndItsNewValue.newValue;
                        break;
                    case ConfigurationOptions.EnterBinanceAPIKey:
                        Application.SessionUser.BinanceKey = optionAndItsNewValue.newValue;
                        break;
                    case ConfigurationOptions.EnterBinanceSecretKey:
                        Application.SessionUser.BinanceSecretKey = optionAndItsNewValue.newValue;
                        break;
                }
            }

            Navigator?.NavigateTo(typeof(ConfigurationsPresenter));
        }
    }
}
