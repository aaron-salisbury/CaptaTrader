using CaptaTrader.Business.Modules.Trading.DomainServices.BotTrader;
using CaptaTrader.Business.Modules.Trading.DTOs;
using CaptaTrader.Business.Modules.Trading.MessageContracts;
using MassTransit;
using Quartz;

namespace CaptaTrader.Business.Modules.Trading.Jobs;

internal class SeekTradeJob : IJob
{
    private readonly IBus _bus;
    private readonly IBotTrader _botTrader;

    public SeekTradeJob(IBus bus, IBotTrader botTrader)
    {
        _bus = bus;
        _botTrader = botTrader;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        TradeOrderDto? tradeOrder = await _botTrader.AttemptToInitiateTradeAsync();

        if (tradeOrder != null)
        {
            await _bus.Publish<TradeExecuted>(new { TradeOrder = tradeOrder });
        }
    }
}
