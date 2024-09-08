using CaptaTrader.Business.MessageContracts;
using CaptaTrader.Business.Services;
using CaptaTrader.Models;
using MassTransit;
using Quartz;

namespace CaptaTrader.Business.Jobs
{
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
            TradeOrder? tradeOrder = await _botTrader.AttemptToInitiateTradeAsync();

            if (tradeOrder != null)
            {
                await _bus.Publish<TradeExecuted>(new { TradeOrder = tradeOrder });
            }
        }
    }
}
