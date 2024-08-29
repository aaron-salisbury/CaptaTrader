using CaptaTrader.Business.DTOs;
using CaptaTrader.Business.Services;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace CaptaTrader.Presentation.CLI.Presenters
{
    internal class BotPresenter : Presenter
    {
        private BotView? _view;
        private IBotService _botService = Application.DomainServices!.GetRequiredService<IBotService>();

        internal override void Display(View view)
        {
            _view = (BotView)view;

            _view.ShutdownRequested += BotView_ShutdownRequested;
            _botService.TradeExecuted += BotService_TradeExecuted;

            if (_botService.Startup())
            {
                _view.Show();
            }
            else
            {
                _view.PopupActivationError();
                Navigator?.NavigateTo(typeof(MainMenuPresenter));
            }
        }

        internal override void Dismiss()
        {
            if (_view != null)
            {
                _view.ShutdownRequested -= BotView_ShutdownRequested;
            }

            _botService.TradeExecuted -= BotService_TradeExecuted;
        }

        private void BotService_TradeExecuted(object? sender, Trade e)
        {
            _view?.PopupTradeOrder(e);
        }

        private void BotView_ShutdownRequested(object? sender, EventArgs e)
        {
            _botService.Shutdown();

            Navigator?.NavigateTo(typeof(MainMenuPresenter));
        }
    }
}
