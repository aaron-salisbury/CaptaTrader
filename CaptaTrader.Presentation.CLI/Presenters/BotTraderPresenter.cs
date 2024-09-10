using CaptaTrader.Business.MessageContracts;
using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Presentation.CLI.Base.MVP;
using CaptaTrader.Presentation.CLI.Views;
using MassTransit;
using System;
using System.Threading.Tasks;

namespace CaptaTrader.Presentation.CLI.Presenters;

internal class BotTraderPresenter : Presenter, IConsumer<TradeExecuted>
{
    private BotTraderView? _view;
    private ITradingService _tradingService;

    public BotTraderPresenter(ITradingService tradingService)
    {
        _tradingService = tradingService;
    }

    internal override void Display(View view)
    {
        _view = (BotTraderView)view;
        _view.ShutdownRequested += BotTraderView_ShutdownRequested;

        _tradingService.InitializeTradingAsync();
        _view.Show();
    }

    internal override void Dismiss()
    {
        if (_view != null)
        {
            _view.ShutdownRequested -= BotTraderView_ShutdownRequested;
        }
    }

    private void BotTraderView_ShutdownRequested(object? sender, EventArgs e)
    {
        Task.Run(_tradingService.ShutdownTradingAsync).Wait();

        Navigator?.NavigateTo(typeof(MainMenuPresenter));
    }

    public Task Consume(ConsumeContext<TradeExecuted> context)
    {
        _view?.PopupTradeOrder(context.Message.TradeOrder);

        return Task.CompletedTask;
    }
}
