using CaptaTrader.Business.Modules.Trading.ApplicationServices;
using CaptaTrader.Business.Modules.Trading.DomainServices;
using CaptaTrader.Business.Modules.Trading.ExternalServices;
using CaptaTrader.Business.Modules.UserAccess.ApplicationServices;
using CaptaTrader.Business.Modules.UserAccess.DomainServices;
using CaptaTrader.Data;
using FluentValidation;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Reflection;

namespace CaptaTrader.Business;

public static class Startup
{
    /// <summary>
    /// Adds business-tier services, including <see cref="DomainInitializer"/> for safely starting & shutting down the business services.
    /// Dependent on <see cref="ILogger"/>, <see cref="ISessionValueProvider"/>, and everything that implements <see cref="IConsumer"/>.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        //TODO: Look Into:
        // APIs
        // https://unplu.gg/test_api.html
        // https://site.financialmodelingprep.com/developer/docs
        // https://www.browsercat.com/pricing
        // https://alternative.me/crypto/api/
        // https://stackoverflow.com/questions/249760/how-can-i-convert-a-unix-timestamp-to-datetime-and-vice-versa

        // Architecture
        // "Validation should be done for api DTOs. And I don’t make objects with complex behavior. Either data objects that have no logic or static (or effectively static) services that do things. This simplifies arch significantly."
        // https://stackoverflow.com/a/12078590
        // https://www.youtube.com/watch?v=ZA2X1gaAhJk
        // https://codeopinion.com/restructuring-to-a-vertical-slice-architecture/
        // https://www.youtube.com/watch?v=T-EwN9UqRwE
        // https://github.com/ardalis/CleanArchitecture/tree/main
        // https://www.thereformedprogrammer.net/creating-domain-driven-design-entity-classes-with-entity-framework-core/
        // I could probably learn a lot from https://github.com/kgrzybek/modular-monolith-with-ddd

        // Mapping
        // https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/ddd-oriented-microservice#layers-in-ddd-microservices
        // https://mapperly.riok.app/docs/intro/




        // Necessary infrastructure.
        services.AddWebRequesting()
            .AddDataAccess()
            .AddScoped<SessionValueResolver, SessionValueResolver>()
            .AddMessaging()
            .AddScheduling();

        // External API services.
#if DEBUG //TODO: Use command option MockExternalAPICalls to decide instead.
        services.AddScoped<IMarketDataService, MockDataService>()
                .AddScoped<IExchangeService, MockExchangeService>()
                .AddScoped<ICryptoFearAndGreedIndex, MockCFGIService>();
#else
        services.AddScoped<IMarketDataService, CoinApiDataService>() //TODO: Potential alternate if limit is reached: https://openapi.coinstats.app/
            .AddScoped<IExchangeService, BinanceExchangeService>()
            .AddScoped<ICryptoFearAndGreedIndex, AlternativeCFGI>();
#endif

        // Internal business domain logic.
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly())
            .AddScoped<ITradeAnalyzerService, MockTradeAnalyzerService>() //TODO: Obviously will need a real analyzer.
            .AddScoped<IBotTrader, BitcoinBotTrader>();

        // Orchestrated public-facing services.
        services.AddScoped<IUserAccessService, UserAccessService>()
            .AddScoped<ITradingService, TradingService>()
            .AddSingleton<DomainInitializer, DomainInitializer>();

        return services;
    }

    private static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        Type[] consumers = services
            .Where(s => typeof(IConsumer).IsAssignableFrom(s.ServiceType))
            .Select(s => s.ServiceType)
            .ToArray();

        services.AddMassTransit(c =>
        {
            c.AddConsumers(consumers);
            c.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); });
        });

        return services;
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
