using CaptaTrader.Business.Services;
using Quartz;

namespace CaptaTrader.Business.Jobs
{
    public class AttemptToInitiateTradeJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            BitcoinBotTraderService bitcoinTradingBot = (BitcoinBotTraderService)context.Scheduler.Context.Get(nameof(BitcoinBotTraderService));

            await bitcoinTradingBot.AttemptToInitiateTradeAsync();
        }
    }
}
