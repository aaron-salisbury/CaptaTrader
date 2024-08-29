using CaptaTrader.Business.Services;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using Microsoft.Extensions.DependencyInjection;
using static CaptaTrader.Presentation.CLI.Views.LoginView;

namespace CaptaTrader.Presentation.CLI.Presenters
{
    internal class LoginPresenter : Presenter
    {
        private LoginView? _view;
        private IUserService _userService = Application.DomainServices!.GetRequiredService<IUserService>();

        internal override void Display(View view)
        {
            _view = (LoginView)view;

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

        private void LoginView_LoginOptionSelected(object? sender, LoginViewEventArgs e)
        {
            switch (e.MenuOption)
            {
                case LoginMenuOptions.Login:
                    _view?.PopupAuthenticate();
                    break;
                case LoginMenuOptions.CreateNewUser:
                    _view?.PopupRegister();
                    break;
                default:
                    Serilog.Log.Warning("Invalid login option selected.");
                    Navigator?.NavigateTo(typeof(LoginPresenter));
                    break;
            }
        }

        private void LoginView_LoginCredentialsEntered(object? sender, LoginViewEventArgs e)
        {
            Application.SessionUser = e.NetworkCredential != null 
                ? _userService.Authenticate(e.NetworkCredential) 
                : null;

            if (Application.SessionUser != null)
            {
                Navigator?.NavigateTo(typeof(MainMenuPresenter));
            }
            else
            {
                _view?.PopupLoginError(nameof(IUserService.Authenticate));
                Navigator?.NavigateTo(typeof(LoginPresenter));
            }
        }

        private void LoginView_RegisterNewCredentialsEntered(object? sender, LoginViewEventArgs e)
        {
            Application.SessionUser = e.NetworkCredential != null
                ? _userService.Register(e.NetworkCredential)
                : null;

            if (Application.SessionUser != null)
            {
                Navigator?.NavigateTo(typeof(MainMenuPresenter));
            }
            else
            {
                _view?.PopupLoginError(nameof(IUserService.Register));
                Navigator?.NavigateTo(typeof(LoginPresenter));
            }
        }
    }
}
