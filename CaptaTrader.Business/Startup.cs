using CaptaTrader.Business.Base;
using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services;
using CaptaTrader.Data.Base.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Reflection;

namespace CaptaTrader.Business
{
    public static class Startup
    {
        public const string COMPRESSION_CLIENT_NAME = "CompressionClient";

        public static IServiceProvider InitializeDomainServices(ILogger logger, ISessionValueProvider sessionValueProvider, bool useMockData)
        {
            IServiceCollection services = new ServiceCollection();

            // Infrastructure services.
            services.AddSingleton(logger)
                .AddSingleton(sessionValueProvider)
                .AddDataAccessEmbedded(Assembly.GetExecutingAssembly().GetEntityTypes())
                .AddHttpClient(COMPRESSION_CLIENT_NAME, c => { c.DefaultRequestHeaders.Add("Accept-Encoding", "deflate, gzip"); })
                    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        AllowAutoRedirect = false,
                        AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
                    });

            // External API services.
            if (useMockData)
            {
                services.AddSingleton<IMarketDataService, MockDataService>()
                    .AddSingleton<IExchangeService, MockExchangeService>()
                    .AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>();
            }
            else
            {
                services.AddSingleton<IMarketDataService, CoinApiDataService>()
                    .AddSingleton<IExchangeService, BinanceExchangeService>()
                    .AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>();//TODO: Obviously will need a real analyzer.
            }

            // Business domain services.
            services.AddSingleton<IBotTraderService, BitcoinBotTraderService>()
                .AddScoped<IUserService, UserService>()
                .AddScoped<IBotService, BotService>();

            return services.BuildServiceProvider();
        }
    }
}
