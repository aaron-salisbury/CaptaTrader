using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services;
using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Data;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CaptaTrader.Business
{
    public static class Startup
    {
        /// <summary>
        /// Initializes and adds public-facing business-tier services, including <see cref="IBusControl"/> for messaging.
        /// Dependent on application <see cref="ILogger"/> and <see cref="ISessionValueProvider"/>.
        /// </summary>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services, bool useMockExternalAPICalls, params Type[] massTransitConsumerTypes)
        {
            //TODO: Look into https://code-maze.com/schedule-jobs-with-quartz-net/

            // Infrastructure services.
            services.AddWebRequesting()
                .AddDataAccessEmbedded(Assembly.GetExecutingAssembly().GetEntityTypes())
                .AddSingleton<ISessionValueResolver, SessionValueResolver>()
                .AddMassTransit(x =>
                {
                    x.AddConsumers(massTransitConsumerTypes);
                    x.UsingInMemory((context, cfg) => { cfg.ConfigureEndpoints(context); });
                });

            // External API services.
            if (useMockExternalAPICalls)
            {
                services.AddSingleton<IMarketDataService, MockDataService>()
                    .AddSingleton<IExchangeService, MockExchangeService>()
                    .AddSingleton<ICryptoFearAndGreedIndex, MockCFGIService>();
            }
            else
            {
                services.AddSingleton<IMarketDataService, CoinApiDataService>() //TODO: Potential alternate if limit is reached: https://openapi.coinstats.app/
                    .AddSingleton<IExchangeService, BinanceExchangeService>()
                    .AddSingleton<ICryptoFearAndGreedIndex, AlternativeCFGI>();
            }

            // Internal business domain logic.
            services.AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>() //TODO: Obviously will need a real analyzer.
                .AddSingleton<IBotTrader, BitcoinBotTrader>();

            // Orchestrated public facing services.
            services.AddSingleton<IUserService, UserService>()
                .AddSingleton<ITradingService, TradingService>();

            return services;
        }
    }
}
