using CaptaTrader.Business.Jobs;
using Quartz;
using Quartz.Impl.Matchers;

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
            IReadOnlyCollection<string> triggerGroupsNames = await _scheduler.GetTriggerGroupNames();
            if (!triggerGroupsNames.Any(tgn => string.Equals(tgn, nameof(TradingService))))
            {
                await ScheduleJobsAsync(_scheduler);
            }
        }

        public async Task ShutdownTradingAsync()
        {
            IReadOnlyCollection<TriggerKey> triggers = await _scheduler
                .GetTriggerKeys(GroupMatcher<TriggerKey>
                    .GroupEquals(nameof(TradingService)));

            await _scheduler.UnscheduleJobs(triggers);
        }

        private static async Task ScheduleJobsAsync(IScheduler scheduler)
        {
            const int MIN_SEC_BETWEEN_TRADES = 6;

            #region SeekTradeJob
            ITrigger seekTradeTrigger = TriggerBuilder.Create()
                .WithIdentity($"{nameof(SeekTradeJob)}Trigger", nameof(TradingService))
                .ForJob(new JobKey(nameof(SeekTradeJob), nameof(TradingService)))
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(MIN_SEC_BETWEEN_TRADES)
                    .RepeatForever())
                .WithDescription($"Trigger configured for a job that seeks to make a trade every {MIN_SEC_BETWEEN_TRADES} seconds.")
                .Build();

            await scheduler.ScheduleJob(seekTradeTrigger);
            #endregion

            #region SeekTradeJob
            //ITrigger bitcoinPriceIntervalsTrigger = TriggerBuilder.Create()
            //    .WithIdentity($"{nameof(BitcoinPriceIntervalsJob)}Trigger", nameof(TradingService))
            //    .ForJob(new JobKey(nameof(BitcoinPriceIntervalsJob), nameof(TradingService)))
            //    .StartAt(new DateTimeOffset(DateTime.Now.Date.AddDays(1)))
            //    .WithSimpleSchedule(x => x
            //        .WithIntervalInHours(24)
            //        .RepeatForever())
            //    .WithDescription($"Trigger configured to update the history of Bitcoin prices daily.")
            //    .Build();

            //await scheduler.ScheduleJob(bitcoinPriceIntervalsTrigger);
            #endregion
        }

        internal static void ConfigureJobs(IServiceCollectionQuartzConfigurator configure)
        {
            JobKey seekTradeJobKey = new(nameof(SeekTradeJob), nameof(TradingService));
            configure.AddJob<SeekTradeJob>(seekTradeJobKey, configure => configure.StoreDurably(true));

            //JobKey bitcoinPriceIntervalsJobKey = new(nameof(BitcoinPriceIntervalsJob), nameof(TradingService));
            //configure.AddJob<BitcoinPriceIntervalsJob>(bitcoinPriceIntervalsJobKey, configure => configure.StoreDurably(true));
        }
    }
}
