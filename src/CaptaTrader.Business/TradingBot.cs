using CaptaTrader.Business.Base;
using CaptaTrader.Business.Modules;
using CaptaTrader.Business.Modules.Trading.Jobs;
using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CaptaTrader.Business;

public sealed class TradingBot : IAsyncDisposable
{
    private static TradingBot? _instance;
    public static TradingBot Instance
    {
        get
        {
            if (_instance == null) { throw new InvalidOperationException("TradingBot has not been initialized. Call Initialize first."); }
            return _instance;
        }
    }

    public int? SessionEndUserId { get; set; }

    public IEventSystem EventBus { get; }
    public IUserAccessService UserAccess { get; }

    private static readonly object _lock = new();
    private readonly IScheduler _scheduler;
    private bool _isTradingInitialized = false;

    private TradingBot(TradingBotConfig tradingBotConfig, ILogger applicationLogger)
    {
        IServiceProvider services = TradingBotBuilder.Build(tradingBotConfig, applicationLogger);

        EventBus = services.GetRequiredService<IEventSystem>();
        UserAccess = services.GetRequiredService<IUserAccessService>();

        _scheduler = services.GetRequiredService<IScheduler>();
    }

    public static void Initialize(TradingBotConfig tradingBotConfig, ILogger applicationLogger)
    {
        lock (_lock)
        {
            if (_instance != null) { throw new InvalidOperationException("TradingBot has already been initialized."); }
            _instance = new TradingBot(tradingBotConfig, applicationLogger);
        }
    }

    public async Task StartAsync()
    {
        await _scheduler.Start();
    }

    public async Task InitializeTradingAsync()
    {
        _isTradingInitialized = true;
        await InitializeTradingJobsAsync();
    }

    public async Task ShutdownTradingAsync()
    {
        if (_isTradingInitialized)
        {
            //TODO: Set a cancellation token to stop the trading service gracefully.
            _isTradingInitialized = false;
            await ShutdownTradingJobsAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await ShutdownTradingAsync();
        await _scheduler.Shutdown().ConfigureAwait(false);

        EventBus.Dispose();
    }

    public async Task InitializeTradingJobsAsync()
    {
        IReadOnlyCollection<string> triggerGroupsNames = await _scheduler.GetTriggerGroupNames();
        if (!triggerGroupsNames.Any(tgn => string.Equals(tgn, nameof(TradingBot))))
        {
            await ScheduleJobsAsync(_scheduler);
        }
    }

    public async Task ShutdownTradingJobsAsync()
    {
        IReadOnlyCollection<TriggerKey> triggers = await _scheduler
            .GetTriggerKeys(GroupMatcher<TriggerKey>
                .GroupEquals(nameof(TradingBot)));

        await _scheduler.UnscheduleJobs(triggers);
    }

    private static async Task ScheduleJobsAsync(IScheduler scheduler)
    {
        const int MIN_SEC_BETWEEN_TRADES = 6;

        #region SeekTradeJob
        ITrigger seekTradeTrigger = TriggerBuilder.Create()
            .WithIdentity($"{nameof(SeekTradeJob)}Trigger", nameof(TradingBot))
            .ForJob(new JobKey(nameof(SeekTradeJob), nameof(TradingBot)))
            .StartNow()
            .WithSimpleSchedule(x => x
                .WithIntervalInSeconds(MIN_SEC_BETWEEN_TRADES)
                .RepeatForever())
            .WithDescription($"Trigger configured for a job that seeks to make a trade every {MIN_SEC_BETWEEN_TRADES} seconds.")
            .Build();

        await scheduler.ScheduleJob(seekTradeTrigger);
        #endregion

        #region BitcoinPriceIntervalsJob
        //ITrigger bitcoinPriceIntervalsTrigger = TriggerBuilder.Create()
        //    .WithIdentity($"{nameof(BitcoinPriceIntervalsJob)}Trigger", nameof(TradingBot))
        //    .ForJob(new JobKey(nameof(BitcoinPriceIntervalsJob), nameof(TradingBot)))
        //    .StartAt(new DateTimeOffset(DateTime.Now.Date.AddDays(1)))
        //    .WithSimpleSchedule(x => x
        //        .WithIntervalInHours(24)
        //        .RepeatForever())
        //    .WithDescription($"Trigger configured to update the history of Bitcoin prices daily.")
        //    .Build();

        //await scheduler.ScheduleJob(bitcoinPriceIntervalsTrigger);
        #endregion
    }
}
