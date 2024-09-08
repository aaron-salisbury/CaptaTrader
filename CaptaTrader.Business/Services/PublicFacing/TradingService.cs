using Quartz;

namespace CaptaTrader.Business.Services.Domain
{
    public class TradingService : ITradingService
    {
        private readonly IScheduler _scheduler;

        public TradingService(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public async Task InitializeTradingAsync()
        {
            await _scheduler.Start();
        }

        public async Task ShutdownTradingAsync()
        {
            await _scheduler.Standby();
        }
    }
}
