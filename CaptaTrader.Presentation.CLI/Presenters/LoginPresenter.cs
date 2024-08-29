using CaptaTrader.Business.Services;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using static CaptaTrader.Presentation.CLI.Views.LoginView;

namespace CaptaTrader.Presentation.CLI.Presenters
{
    internal class LoginPresenter : Presenter
    {
        private LoginView? _view;
        private IUserService? _userService = Application.DomainServices?.GetRequiredService<IUserService>();

        internal override void Display()
        {
            _view = ViewLocator.GetView<LoginView>(this);

            _view.LoginOptionSelected += LoginView_LoginOptionSelected;
            _view.LoginCredentialsEntered += LoginView_LoginCredentialsEntered;
            _view.RegisterNewCredentialsEntered += LoginView_RegisterNewCredentialsEntered;

            _view.Show();
        }

        internal override void Dismiss()
        {
            if (_view != null)
            {
                _view.LoginOptionSelected -= LoginView_LoginOptionSelected;
                _view.LoginCredentialsEntered -= LoginView_LoginCredentialsEntered;
                _view.LoginCredentialsEntered -= LoginView_RegisterNewCredentialsEntered;
            }
        }

        private void LoginView_LoginOptionSelected(object? sender, LoginMenuOptions selectedOption)
        {
            switch (selectedOption)
            {
                case LoginMenuOptions.Login:
                    _view?.RequestLogin();
                    break;
                case LoginMenuOptions.CreateNewUser:
                    _view?.RegisterNew();
                    break;
            }
        }

        private void LoginView_LoginCredentialsEntered(object? sender, NetworkCredential credentials)
        {
            Application.SessionUser = _userService?.Authenticate(credentials);

            if (Application.SessionUser != null)
            {
                Navigator?.NavigateTo(typeof(MainMenuPresenter));
            }
            else
            {
                //TODO: Request from the view a bad login attempt and then navigate to a new login view.
            }
        }

        private void LoginView_RegisterNewCredentialsEntered(object? sender, NetworkCredential credentials)
        {
            Application.SessionUser = _userService?.Register(credentials);

            if (Application.SessionUser != null)
            {
                Navigator?.NavigateTo(typeof(MainMenuPresenter));
            }
            else
            {
                //TODO: Request from the view a bad login attempt and then navigate to a new login view.
            }
        }
    }
}
