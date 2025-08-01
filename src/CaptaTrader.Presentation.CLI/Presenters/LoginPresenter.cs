using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using CaptaTrader.Presentation.CLI.Views;
using RunnethOverStudio.AppToolkit.Core;
using RunnethOverStudio.AppToolkit.Presentation.MVP;
using System;
using static CaptaTrader.Presentation.CLI.Views.LoginView;

namespace CaptaTrader.Presentation.CLI.Presenters;

internal sealed class LoginPresenter(Navigator navigator) : Presenter(navigator)
{
    private LoginView? _view;

    public override void Display(View view)
    {
        _view = (LoginView)view;

        _view.LoginOptionSelected += LoginView_LoginOptionSelected;
        _view.LoginCredentialsEntered += LoginView_LoginCredentialsEntered;
        _view.RegisterNewCredentialsEntered += LoginView_RegisterNewCredentialsEntered;

        _view.Show();
    }

    public override void Dismiss()
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
        ProcessResult result = e.NetworkCredential == null
            ? new ProcessResult([new Exception("No credentials found.")])
            : Application.TradingBot.UserAccess.Authenticate(e.NetworkCredential);
   
        if (result.IsValid)
        {
            Navigator.NavigateTo(typeof(MainMenuPresenter));
        }
        else
        {
            _view?.ShowProcessFail(nameof(IUserAccessService.Authenticate), result);
            _view?.Show();
        }
    }

    private void LoginView_RegisterNewCredentialsEntered(object? sender, LoginViewEventArgs e)
    {
        ProcessResult result = e.NetworkCredential == null
            ? new ProcessResult([new Exception("No credentials found.")])
            : Application.TradingBot.UserAccess.Register(e.NetworkCredential);
            
        if (result.IsValid)
        {
            Navigator.NavigateTo(typeof(MainMenuPresenter));
        }
        else
        {
            _view?.ShowProcessFail(nameof(IUserAccessService.Register), result);
            _view?.Show();
        }
    }
}
