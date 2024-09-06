using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services;
using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CaptaTrader.Business
{
    public static class Startup
    {
        /// <summary>
        /// Initializes and adds business-tier services.
        /// Dependant on application ILogger and ISessionValueProvider.
        /// </summary>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services, ILogger logger, ISessionValueProvider session, bool useMockExternalAPICalls)
        {
            IServiceCollection internalServices = new ServiceCollection();

            // Application services.
            internalServices.AddSingleton(logger);
            internalServices.AddSingleton(session);

            // Infrastructure services.
            internalServices.AddWebRequesting()
                .AddDataAccessEmbedded(Assembly.GetExecutingAssembly().GetEntityTypes())
                .AddSingleton<ISessionValueResolver, SessionValueResolver>();

            // External API services.
            if (useMockExternalAPICalls)
            {
                internalServices.AddSingleton<IMarketDataService, MockDataService>()
                    .AddSingleton<IExchangeService, MockExchangeService>()
                    .AddSingleton<ICryptoFearAndGreedIndex, MockCFGIService>();
            }
            else
            {
                internalServices.AddSingleton<IMarketDataService, CoinApiDataService>() //TODO: Potential alternate if limit is reached: https://openapi.coinstats.app/
                    .AddSingleton<IExchangeService, BinanceExchangeService>()
                    .AddSingleton<ICryptoFearAndGreedIndex, AlternativeCFGI>();
            }

            // Internal business domain logic.
            internalServices.AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>() //TODO: Obviously will need a real analyzer.
                .AddSingleton<IBotTrader, BitcoinBotTrader>();

            // Orchestrated public facing services.
            internalServices.AddSingleton<IUserService, UserService>()
                .AddSingleton<ITradingService, TradingService>();

            IServiceProvider internalProvider = internalServices.BuildServiceProvider();

            return services.AddSingleton(internalProvider.GetRequiredService<IUserService>())
                .AddSingleton(internalProvider.GetRequiredService<ITradingService>());
        }
    }
}
