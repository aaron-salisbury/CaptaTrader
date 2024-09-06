using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI.Presenters
{
    internal class BotTraderPresenter : Presenter
    {
        private BotTraderView? _view;
        private ITradingService _botTrader;

        public BotTraderPresenter(ITradingService botTrader)
        {
            _botTrader = botTrader;
            _botTrader.RunningPortfolio.PropertyChanged += Portfolio_PropertyChanged;
        }

        internal override void Display(View view)
        {
            _view = (BotTraderView)view;
            _view.ShutdownRequested += BotTraderView_ShutdownRequested;

            if (_botTrader.InitializeTradingAsync().Result)
            {
                _view.Show();
            }
            else
            {
                _view?.PopupProcessFail("Bot activation");
                Navigator?.NavigateTo(typeof(MainMenuPresenter));
            }
        }

        internal override void Dismiss()
        {
            if (_view != null)
            {
                _view.ShutdownRequested -= BotTraderView_ShutdownRequested;
            }

            _botTrader.RunningPortfolio.PropertyChanged -= Portfolio_PropertyChanged;
        }

        private void BotTraderView_ShutdownRequested(object? sender, EventArgs e)
        {
            Task.Run(_botTrader.ShutdownTradingAsync).Wait();

            Navigator?.NavigateTo(typeof(MainMenuPresenter));
        }

        private void Portfolio_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is Portfolio portfolio)
            {
                switch (e.PropertyName)
                {
                    case nameof(Portfolio.LastCompletedTradeOrder):
                        if (portfolio.LastCompletedTradeOrder != null) { _view?.PopupTradeOrder(portfolio.LastCompletedTradeOrder); }
                        break;
                }
            }
        }
    }
}
