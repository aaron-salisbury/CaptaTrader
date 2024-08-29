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

        //private Notification? _lastNotification;
        //public Notification? LastNotification
        //{
        //    get => _lastNotification;
        //    set => SetProperty(ref _lastNotification, value);
        //}

        //internal void Notify(Notification notification)
        //{
        //    LastNotification = notification;
        //}

        //internal void Notify(string message)
        //{
        //    Notify(new Notification()
        //    {
        //        NotificationType = Notification.NotificationTypes.Message,
        //        Message = message
        //    });
        //}

        public static IServiceProvider InitializeDomainServices(ILogger logger, bool useMockData)
        {
            return ConfigureServices(logger, useMockData)
                .BuildServiceProvider();
        }

        private static ServiceCollection ConfigureServices(ILogger logger, bool useMockData)
        {
            const string COMPRESSION_CLIENT_NAME = "CompressionClient";

            ServiceCollection services = new();

            logger.LogDebug("Beginning to configure domain services...");

            logger.LogDebug("... Data access service");
            services.AddDataAccessEmbedded(Assembly.GetExecutingAssembly().GetEntityTypes());

            logger.LogDebug("... Logger service");
            services.AddSingleton(typeof(ILogger), logger);

            logger.LogDebug("... Http service");
            services.AddHttpClient(COMPRESSION_CLIENT_NAME, c =>
            {
                c.DefaultRequestHeaders.Add("Accept-Encoding", "deflate, gzip");
            })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
                });

            logger.LogDebug("... External API services");
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

            logger.LogDebug("... Bot service");
            services.AddSingleton<IBotTraderService, BitcoinBotTraderService>();

            logger.LogDebug("... public services");
            services.AddSingleton<IUserService, UserService>();
            services.AddSingleton<IBotService, BotService>();

            return services;
        }
    }
}
