using CaptaTrader.Business.Models;
using Quartz;

namespace CaptaTrader.Business.Jobs
{
    public class BitcoinPriceIntervalsJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            BitcoinTrader bitcoinTradingBot = (BitcoinTrader)context.Scheduler.Context.Get(nameof(BitcoinTrader));

            //await bitcoinTradingBot.UpdatePriceIntervalsAsync();

            //TODO: TESTING - Pretend to UpdatePriceIntervalsAsync().
            Console.WriteLine("Pretending to update price histories...");
            await Task.Delay(5000);
        }
    }
}
