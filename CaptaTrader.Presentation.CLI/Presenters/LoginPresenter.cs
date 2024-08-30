using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
            IEnumerable<ValidationResult> results;

            if (e.NetworkCredential == null)
            {
                Application.SessionUser = null;
                results = [new ValidationResult("No credentials found.")];
            }
            else
            {
                (EndUser?, IEnumerable<ValidationResult>) result = _userService.Authenticate(e.NetworkCredential);
                Application.SessionUser = result.Item1;
                results = result.Item2;
            }

            if (Application.SessionUser != null)
            {
                Navigator?.NavigateTo(typeof(MainMenuPresenter));
            }
            else
            {
                _view?.PopupProcessFail(nameof(IUserService.Authenticate), results);
                Navigator?.NavigateTo(typeof(LoginPresenter));
            }
        }

        private void LoginView_RegisterNewCredentialsEntered(object? sender, LoginViewEventArgs e)
        {
            IEnumerable<ValidationResult> results;

            if (e.NetworkCredential == null)
            {
                Application.SessionUser = null;
                results = [new ValidationResult("No credentials found.")];
            }
            else
            {
                (EndUser?, IEnumerable<ValidationResult>) result = _userService.Authenticate(e.NetworkCredential);
                Application.SessionUser = result.Item1;
                results = result.Item2;
            }

            if (Application.SessionUser != null)
            {
                Navigator?.NavigateTo(typeof(MainMenuPresenter));
            }
            else
            {
                _view?.PopupProcessFail(nameof(IUserService.Register), results);
                Navigator?.NavigateTo(typeof(LoginPresenter));
            }
        }
    }
}
