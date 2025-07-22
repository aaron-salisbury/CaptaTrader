using CaptaTrader.Business.Modules.EventSystemEvents;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using System;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI.Presenters;

internal sealed class BotTraderPresenter(Navigator navigator) : Presenter(navigator)
{
    private BotTraderView? _view;

    internal override void Display(View view)
    {
        _view = (BotTraderView)view;
        _view.ShutdownRequested += BotTraderView_ShutdownRequested;

        Application.TradingBot.EventBus.Subscribe<TradeExecutedEventArgs>(OnTradeExecuted);

        Task.Run(Application.TradingBot.InitializeTradingAsync);
        _view.Show();
    }

    internal override void Dismiss()
    {
        Application.TradingBot.EventBus.Unsubscribe<TradeExecutedEventArgs>(OnTradeExecuted);

        if (_view != null)
        {
            _view.ShutdownRequested -= BotTraderView_ShutdownRequested;
        }
    }

    private void BotTraderView_ShutdownRequested(object? sender, EventArgs e)
    {
        Task.Run(Application.TradingBot.ShutdownTradingAsync).Wait();

        Navigator.NavigateTo(typeof(MainMenuPresenter));
    }

    private void OnTradeExecuted(object? sender, TradeExecutedEventArgs e)
    {
        _view?.PopupTradeOrder(e.TradeOrder);
    }
}
