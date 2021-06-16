using Quartz;
using System.Threading.Tasks;

namespace CaptaTrader.Core.Jobs
{
    public class BitcoinPriceIntervalsJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            BitcoinTradingBot bitcoinTradingBot = (BitcoinTradingBot)context.Scheduler.Context.Get(nameof(BitcoinTradingBot));

            await bitcoinTradingBot.UpdateBitcoinPriceIntervalsAsync();
        }
    }
}
