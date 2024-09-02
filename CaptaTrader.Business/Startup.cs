using CaptaTrader.Business.Base;
using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services;
using CaptaTrader.Data.Base.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CaptaTrader.Business
{
    public static class Startup
    {
        public const string COMPRESSION_CLIENT_NAME = "CompressionClient";

        public static IServiceCollection RegisterDomainServices(ILogger logger, ISessionValueProvider sessionValueProvider, bool useMockData)
        {
            IServiceCollection services = new ServiceCollection();

            // Infrastructure services.
            services.AddSingleton(logger)
                .AddSingleton(sessionValueProvider)
                .AddWebRequesting()
                .AddDataAccessEmbedded(Assembly.GetExecutingAssembly().GetEntityTypes());

            // External API services.
            if (useMockData)
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
                .AddSingleton<IBotTraderService, BitcoinBotTraderService>()
                .AddScoped<IUserService, UserService>();

            return services;
        }
    }
}
