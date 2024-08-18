using CaptaTrader.Business.Services;
using Quartz;

namespace CaptaTrader.Business.Jobs
{
    public class BitcoinPriceIntervalsJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            BitcoinTradingService bitcoinTradingBot = (BitcoinTradingService)context.Scheduler.Context.Get(nameof(BitcoinTradingService));

            await bitcoinTradingBot.UpdatePriceIntervalsAsync();
        }
    }
}
