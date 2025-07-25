using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using static CaptaTrader.Presentation.CLI.Views.MainMenuView;

namespace CaptaTrader.Presentation.CLI.Presenters;

internal sealed class MainMenuPresenter : Presenter
{
    private MainMenuView? _view;

    public MainMenuPresenter(Navigator navigator) : base(navigator) { }

    internal override void Display(View view)
    {
        _view = (MainMenuView)view;

        _view.MainMenuOptionSelected += MainMenuView_MainMenuOptionSelected;

        _view.Show();
    }

    internal override void Dismiss()
    {
        if (_view != null)
        {
            _view.MainMenuOptionSelected -= MainMenuView_MainMenuOptionSelected;
        }
    }

    private void MainMenuView_MainMenuOptionSelected(object? sender, MainMenuViewEventArgs e)
    {
        switch (e.MenuOption)
        {
            case MainMenuOptions.LaunchBot:
                Navigator.NavigateTo(typeof(BotTraderPresenter));
                break;
            case MainMenuOptions.EnterConfigurations:
                Navigator.NavigateTo(typeof(ConfigurationsPresenter));
                break;
            case MainMenuOptions.Utilities:
                Navigator.NavigateTo(typeof(UtilitiesPresenter));
                break;
            case MainMenuOptions.Exit:
                return;
        }
    }
}
