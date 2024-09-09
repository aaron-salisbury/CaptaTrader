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
            //await _scheduler
            //    .ResumeTriggers(GroupMatcher<TriggerKey>
            //        .GroupEquals(nameof(BitcoinBotTrader)));

            //await _scheduler.Start();

            await ScheduleJobsAsync(_scheduler);
        }

        public async Task ShutdownTradingAsync()
        {
            //await _scheduler
            //    .PauseTriggers(GroupMatcher<TriggerKey>
            //        .GroupEquals(nameof(BitcoinBotTrader)));
            
            //await _scheduler.Standby();

            IReadOnlyCollection<TriggerKey> triggers = await _scheduler
                .GetTriggerKeys(GroupMatcher<TriggerKey>
                    .GroupEquals(nameof(TradingService)));

            await _scheduler.UnscheduleJobs(triggers);
        }

        private static async Task ScheduleJobsAsync(IScheduler scheduler)
        {
            const int MIN_SEC_BETWEEN_TRADES = 6;

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
        }

        internal static void ConfigureJobs(IServiceCollectionQuartzConfigurator configure)
        {
            #region SeekTradeJob
            JobKey seekTradeJobKey = new(nameof(SeekTradeJob), nameof(TradingService));
            configure.AddJob<SeekTradeJob>(seekTradeJobKey, configure => configure.StoreDurably(true));

            // configure..ScheduleJob<SeekTradeJob>(trigger => trigger
            //.WithIdentity($"{nameof(SeekTradeJob)}Trigger", nameof(TradingService))
            //.StartNow()
            //.WithSimpleSchedule(x => x
            //    .WithIntervalInSeconds(MIN_SEC_BETWEEN_TRADES)
            //    .RepeatForever())
            //.WithDescription($"Trigger configured for a job that seeks to make a trade every {MIN_SEC_BETWEEN_TRADES} seconds."));
            #endregion

            #region BitcoinPriceIntervalsJob
            //IJobDetail bitcoinPriceIntervalsJob = JobBuilder.Create<BitcoinPriceIntervalsJob>()
            //    .WithIdentity(nameof(BitcoinPriceIntervalsJob), nameof(TradingService))
            //    .Build();

            //ITrigger bitcoinPriceIntervalsTrigger = TriggerBuilder.Create()
            //    .WithIdentity($"{nameof(BitcoinPriceIntervalsJob)}Trigger", nameof(TradingService))
            //    .StartAt(new DateTimeOffset(DateTime.Now.Date.AddDays(1)))
            //    .WithSimpleSchedule(x => x
            //        .WithIntervalInHours(24)
            //        .RepeatForever())
            //    .Build();

            //await scheduler.ScheduleJob(bitcoinPriceIntervalsJob, bitcoinPriceIntervalsTrigger);
            #endregion

            #region AttemptToInitiateTradeJob
            //IJobDetail attemptToInitiateTradeJob = JobBuilder.Create<AttemptToInitiateTradeJob>()
            //    .WithIdentity(nameof(AttemptToInitiateTradeJob), nameof(TradingService))
            //    .Build();

            //ITrigger attemptToInitiateTradeTrigger = TriggerBuilder.Create()
            //    .WithIdentity($"{nameof(AttemptToInitiateTradeJob)}Trigger", nameof(TradingService))
            //    .StartNow()
            //    .WithSimpleSchedule(x => x
            //        .WithIntervalInSeconds(MIN_SEC_BETWEEN_TRADES)
            //        .RepeatForever())
            //    .Build();

            //await scheduler.ScheduleJob(attemptToInitiateTradeJob, attemptToInitiateTradeTrigger);
            #endregion
        }
    }
}
