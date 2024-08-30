using CaptaTrader.Business.Entities;
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
        internal static User? SessionUser { get; set; } //TODO: I hate this.

        public static IServiceProvider InitializeDomainServices(ILogger logger, bool useMockData)
        {
            return ConfigureServices(logger, useMockData)
                .BuildServiceProvider();
        }

        private static IServiceCollection ConfigureServices(ILogger logger, bool useMockData)
        {
            const string COMPRESSION_CLIENT_NAME = "CompressionClient";

            IServiceCollection services = new ServiceCollection();

            logger.LogDebug("Beginning to configure domain services...");

            logger.LogDebug("... data access service");
            services.AddDataAccessEmbedded(Assembly.GetExecutingAssembly().GetEntityTypes());

            logger.LogDebug("... logger service");
            services.AddSingleton(typeof(ILogger), logger);

            logger.LogDebug("... HTTP service");
            services.AddHttpClient(COMPRESSION_CLIENT_NAME, c =>
            {
                c.DefaultRequestHeaders.Add("Accept-Encoding", "deflate, gzip");
            })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
                });

            logger.LogDebug("... external API services");
            if (useMockData)
            {
                services.AddSingleton<IMarketDataService, MockDataService>();
                services.AddSingleton<IExchangeService, MockExchangeService>();
                services.AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>();
            }
            else
            {
                services.AddSingleton<IMarketDataService, CoinApiDataService>(serviceProvider => new CoinApiDataService(
                    logger: serviceProvider.GetRequiredService<ILogger>(),
                    httpClientFactory: serviceProvider.GetRequiredService<IHttpClientFactory>(),
                    getKeyFunction: () => { return SessionUser?.CoinAPIKey; },
                    compressionClientName: COMPRESSION_CLIENT_NAME));

                services.AddSingleton<IExchangeService, BinanceExchangeService>(serviceProvider => new BinanceExchangeService(
                    logger: serviceProvider.GetRequiredService<ILogger>(),
                    loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>(),
                    httpClientFactory: serviceProvider.GetRequiredService<IHttpClientFactory>(),
                    getKeysFunction: () => { return (SessionUser?.BinanceKey, SessionUser?.BinanceSecretKey); }));

                //TODO: Obviously will need a real analyzer.
                services.AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>();
            }

            logger.LogDebug("... bot service");
            services.AddSingleton<IBotTraderService, BitcoinBotTraderService>();

            logger.LogDebug("... public services");
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IBotService, BotService>();

            return services;
        }
    }
}
