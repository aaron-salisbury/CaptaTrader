using CaptaTrader.Business.Services;
using Quartz;

namespace CaptaTrader.Business.Jobs
{
    public class BitcoinPriceIntervalsJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            BitcoinBotTrader bitcoinTradingBot = (BitcoinBotTrader)context.Scheduler.Context.Get(nameof(BitcoinBotTrader));

            await bitcoinTradingBot.UpdatePriceIntervalsAsync();
        }
    }
}
