using CaptaTrader.Business.Modules.EventSystemEvents;
using CaptaTrader.Business.Modules.Trading.DomainServices;
using CaptaTrader.Business.Modules.Trading.DTOs;
using Quartz;
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
        TradeOrderDto? tradeOrder = await _botTrader.AttemptToInitiateTradeAsync();

        if (tradeOrder != null)
        {
            _bus.Publish(this, new TradeExecutedEventArgs() { TradeOrder = tradeOrder });
        }
    }
}
