using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using FluentValidation.Results;
using static CaptaTrader.Presentation.CLI.Views.LoginView;

namespace CaptaTrader.Presentation.CLI.Presenters;

internal class LoginPresenter : Presenter
{
    private LoginView? _view;
    private IUserAccessService _userService;

    public LoginPresenter(IUserAccessService userService, Navigator navigator) : base(navigator)
    {
        _userService = userService;
    }

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
            case LoginMenuOptions.Exit:
                return;
            default:
                Serilog.Log.Warning("Invalid login option selected.");
                _view?.Show();
                break;
        }
    }

    private void LoginView_LoginCredentialsEntered(object? sender, LoginViewEventArgs e)
    {
        ValidationResult result = e.NetworkCredential == null
            ? new ValidationResult([new ValidationFailure() { ErrorMessage = "No credentials found." }])
            : _userService.Authenticate(e.NetworkCredential);
   
        if (result.IsValid)
        {
            Navigator.NavigateTo(typeof(MainMenuPresenter));
        }
        else
        {
            _view?.PopupProcessFail(nameof(IUserAccessService.Authenticate), result);
            _view?.Show();
        }
    }

    private void LoginView_RegisterNewCredentialsEntered(object? sender, LoginViewEventArgs e)
    {
        ValidationResult result = e.NetworkCredential == null
            ? new ValidationResult([new ValidationFailure() { ErrorMessage = "No credentials found." }])
            : _userService.Register(e.NetworkCredential);
            
        if (result.IsValid)
        {
            Navigator.NavigateTo(typeof(MainMenuPresenter));
        }
        else
        {
            _view?.PopupProcessFail(nameof(IUserAccessService.Register), result);
            _view?.Show();
        }
    }
}
