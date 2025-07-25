using CaptaTrader.Business.Modules;
using CaptaTrader.Business.Modules.Trading.DomainServices;
using CaptaTrader.Business.Modules.Trading.ExternalServices;
using CaptaTrader.Business.Modules.Trading.Jobs;
using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using CaptaTrader.Data;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Reflection;

namespace CaptaTrader.Business.Base;

internal static class TradingBotBuilder
{
    public static IServiceProvider Build(TradingBotConfig tradingBotConfig, ILogger applicationLogger)
    {
        IServiceCollection services = new ServiceCollection();

        // Core Systems
        services.AddSingleton(applicationLogger);
        services.AddSingleton<IEventSystem, EventSystem>();
        services.AddWebRequesting();
        services.AddDataAccess();
        services.AddScheduling();

        // External API services.
        if (tradingBotConfig.UseMockTransactions)
        {
            services.AddSingleton<IMarketDataService, MockDataService>();
            services.AddSingleton<IExchangeService, MockExchangeService>();
            services.AddSingleton<ICryptoFearAndGreedIndex, MockCFGIService>();
        }
        else
        {
            services.AddScoped<IMarketDataService, CoinApiDataService>(); //TODO: Potential alternate if limit is reached: https://openapi.coinstats.app/
            services.AddScoped<IExchangeService, BinanceExchangeService>();
            services.AddScoped<ICryptoFearAndGreedIndex, AlternativeCFGI>();
        }

        // Internal business domain logic.
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<ITradeAnalyzer, MockTradeAnalyzer>(); //TODO: Obviously will need a real analyzer.
        services.AddScoped<IBotTrader, BitcoinBotTrader>();
        services.AddScoped<IReportingService, CSVReportingService>();

        // Top application-level modules.
        services.AddScoped<IUserAccessService, UserAccessService>();

        return services.BuildServiceProvider();
    }

    private static IServiceCollection AddScheduling(this IServiceCollection services)
    {
        services.AddQuartz(c =>
        {
            c.InterruptJobsOnShutdown = true;
            c.UseSimpleTypeLoader();
            c.UseInMemoryStore();

            JobKey seekTradeJobKey = new(nameof(SeekTradeJob), nameof(TradingBot));
            c.AddJob<SeekTradeJob>(seekTradeJobKey, configure => configure.StoreDurably(true));

            //JobKey bitcoinPriceIntervalsJobKey = new(nameof(BitcoinPriceIntervalsJob), nameof(TradingService));
            //configure.AddJob<BitcoinPriceIntervalsJob>(bitcoinPriceIntervalsJobKey, configure => configure.StoreDurably(true));
        });

        services.AddSingleton(sp => sp.GetRequiredService<ISchedulerFactory>().GetScheduler().Result);

        return services;
    }
}
