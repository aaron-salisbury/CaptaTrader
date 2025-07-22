using CaptaTrader.Business.Base;
using CaptaTrader.Business.Modules;
using CaptaTrader.Business.Modules.Trading.ApplicationServices;
using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Threading.Tasks;

namespace CaptaTrader.Business;

public sealed class TradingBot : IAsyncDisposable
{
    public IEventSystem EventBus { get; }
    public IUserAccessService UserAccess { get; }

    private readonly IScheduler _scheduler;
    private readonly ITradingService _tradingService;

    public TradingBot(TradingBotConfig tradingBotConfig, ILogger applicationLogger)
    {
        IServiceProvider services = TradingBotBuilder.Build(tradingBotConfig, applicationLogger);

        EventBus = services.GetRequiredService<IEventSystem>();
        UserAccess = services.GetRequiredService<IUserAccessService>();

        _scheduler = services.GetRequiredService<IScheduler>();
        _tradingService = services.GetRequiredService<ITradingService>();
    }

    public async Task StartAsync()
    {
        await _scheduler.Start();
    }

    public async Task InitializeTradingAsync()
    {
        await _tradingService.InitializeTradingAsync();
    }

    public async Task ShutdownTradingAsync()
    {
        await _tradingService.ShutdownTradingAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _scheduler.Shutdown().ConfigureAwait(false);

        EventBus.Dispose();
    }
}
