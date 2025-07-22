using CaptaTrader.Business.Modules;
using CaptaTrader.Business.Modules.Trading.ApplicationServices;
using CaptaTrader.Business.Modules.Trading.DomainServices;
using CaptaTrader.Business.Modules.Trading.ExternalServices;
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
            services.AddScoped<IMarketDataService, MockDataService>();
            services.AddScoped<IExchangeService, MockExchangeService>();
            services.AddScoped<ICryptoFearAndGreedIndex, MockCFGIService>();
        }
        else
        {
            services.AddScoped<IMarketDataService, CoinApiDataService>(); //TODO: Potential alternate if limit is reached: https://openapi.coinstats.app/
            services.AddScoped<IExchangeService, BinanceExchangeService>();
            services.AddScoped<ICryptoFearAndGreedIndex, AlternativeCFGI>();
        }

        // Internal business domain logic.
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<ITradeAnalyzerService, MockTradeAnalyzerService>(); //TODO: Obviously will need a real analyzer.
        services.AddScoped<IBotTrader, BitcoinBotTrader>();

        // Top application-level modules.
        services.AddScoped<IUserAccessService, UserAccessService>();
        services.AddScoped<ITradingService, TradingService>();

        return services.BuildServiceProvider();
    }

    private static IServiceCollection AddScheduling(this IServiceCollection services)
    {
        services.AddQuartz(c =>
        {
            c.InterruptJobsOnShutdown = true;
            c.UseSimpleTypeLoader();
            c.UseInMemoryStore();

            TradingService.ConfigureJobs(c);
        });

        services.AddSingleton(sp => sp.GetRequiredService<ISchedulerFactory>().GetScheduler().Result);

        return services;
    }
}
