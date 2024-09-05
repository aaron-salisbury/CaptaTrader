using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services;
using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CaptaTrader.Business
{
    public static class Startup
    {
        /// <summary>
        /// Adds all business-tier services.
        /// Logging and ISessionValueProvider services are expected to have already been injected.
        /// </summary>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services, bool useMockExternalAPICalls)
        {
            // Infrastructure services.
            services.AddWebRequesting()
                .AddDataAccessEmbedded(Assembly.GetExecutingAssembly().GetEntityTypes())
                .AddSingleton<ISessionValueResolver, SessionValueResolver>();

            // External API services.
            if (useMockExternalAPICalls)
            {
                services.AddSingleton<IMarketDataService, MockDataService>()
                    .AddSingleton<IExchangeService, MockExchangeService>();
            }
            else
            {
                services.AddSingleton<IMarketDataService, CoinApiDataService>()
                    .AddSingleton<IExchangeService, BinanceExchangeService>();
            }

            // Business domain services.
            services.AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>() //TODO: Obviously will need a real analyzer.
                .AddSingleton<IBotTrader, BitcoinBotTrader>()
                .AddScoped<IUserService, UserService>()
                .AddSingleton<ITradingService, TradingService>();

            return services;
        }
    }
}
