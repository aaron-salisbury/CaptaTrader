using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using static CaptaTrader.Presentation.CLI.Views.MainMenuView;

namespace CaptaTrader.Presentation.CLI.Presenters
{
    internal class MainMenuPresenter : Presenter
    {
        private MainMenuView? _view;

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

        private void MainMenuView_MainMenuOptionSelected(object? sender, MainMenuOptions selectedOption)
        {
            switch (selectedOption)
            {
                case MainMenuOptions.EnterConfigurations:
                    Navigator?.NavigateTo(typeof(ConfigurationsPresenter));
                    break;
                case MainMenuOptions.LaunchBot:
                    Navigator?.NavigateTo(typeof(BotPresenter));
                    break;
                case MainMenuOptions.Exit:
                    return;
            }
        }
    }
}
