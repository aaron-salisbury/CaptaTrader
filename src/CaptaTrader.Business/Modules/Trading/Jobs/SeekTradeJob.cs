using CaptaTrader.Business.Modules.EventSystemEvents;
using CaptaTrader.Business.Modules.Trading.DomainServices;
using CaptaTrader.Business.Modules.Trading.DTOs;
using Quartz;
using RunnethOverStudio.AppToolkit.Modules.Messaging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.Jobs;

internal sealed class SeekTradeJob : IJob
{
    private readonly IEventSystem _bus;
    private readonly IBotTrader _botTrader;

    public SeekTradeJob(IEventSystem bus, IBotTrader botTrader)
    {
        _bus = bus;
        _botTrader = botTrader;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        IEnumerable<TradeOrderDto> tradeOrders = await _botTrader.AttemptToInitiateTradesAsync();

        foreach (TradeOrderDto tradeOrder in tradeOrders)
        {
            _bus.Publish(this, new TradeExecutedEventArgs() { TradeOrder = tradeOrder });
        }
    }
}
