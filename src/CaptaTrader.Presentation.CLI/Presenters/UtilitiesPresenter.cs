using CaptaTrader.Presentation.CLI.Views;
using RunnethOverStudio.AppToolkit.Presentation.MVP;
using System.Threading.Tasks;
using static CaptaTrader.Presentation.CLI.Views.UtilitiesView;

namespace CaptaTrader.Presentation.CLI.Presenters;

internal sealed class UtilitiesPresenter(Navigator navigator) : Presenter(navigator)
{
    private UtilitiesView? _view;

    public override void Display(View view)
    {
        _view = (UtilitiesView)view;

        _view.UtilityOptionSelected += UtilitiesView_UtilityOptionSelected;

        _view.Show();
    }

    public override void Dismiss()
    {
        if (_view != null)
        {
            _view.UtilityOptionSelected -= UtilitiesView_UtilityOptionSelected;
        }
    }

    private void UtilitiesView_UtilityOptionSelected(object? sender, UtilitiesViewEventArgs e)
    {
        switch (e.UtilityOption)
        {
            case UtilitiesOptions.ReportTrades:
                Task.Run(async () => await Application.TradingBot.WriteAllHistoricalTradesAsync()).Wait();
                //TODO: Show a spinner or something until this finishes.
                Navigator.NavigateTo(typeof(MainMenuPresenter));
                break;
            case UtilitiesOptions.Return:
                Navigator.NavigateTo(typeof(MainMenuPresenter));
                break;
            default:
                Serilog.Log.Warning("Invalid utility option selected.");
                Navigator.NavigateTo(typeof(MainMenuPresenter));
                break;
        }
    }
}
