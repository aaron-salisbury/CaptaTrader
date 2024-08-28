using CaptaTrader.Business.Base;
using CaptaTrader.Business.Entities;
using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services;
using CaptaTrader.Data;
using CaptaTrader.Data.Base.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Reflection;
using System.Security;

namespace CaptaTrader.Business
{
    public class AppManager : ObservableModel
    {
        internal static readonly string COMPRESSION_CLIENT_NAME = "CompressionClient";

        public IServiceProvider Services { get; private set; }
        public User? User { get; private set; }

        private Notification? _lastNotification;
        public Notification? LastNotification
        {
            get => _lastNotification;
            set => SetProperty(ref _lastNotification, value);
        }

        private DataAccesor _dataAccesor;
        private IBotTraderService? _botTrader;

        public AppManager(ILogger logger, bool useMockData)
        {
            Services = ConfigureServices(logger, useMockData).BuildServiceProvider();

            _dataAccesor = Services.GetRequiredService<DataAccesor>();
        }

        public bool CreateUser(string userName, SecureString password)
        {
            if (string.IsNullOrEmpty(userName) || _dataAccesor.ReadAll<User>().Any(u => string.Equals(userName, u.UserName)))
            {
                return false;
            }

            User newUser = new() { UserName = userName };
            _dataAccesor.Create(newUser);
            User = newUser;

            AppCredentials credentials = Security.CreateAppCredentials(password);
            credentials.UserId = newUser.Id;
            _dataAccesor.Create(credentials);

            return true;
        }

        public bool VerifyUser(string userName, SecureString password)
        {
            User? user = _dataAccesor.ReadWhere<User>(u => string.Equals(userName, u.UserName)).FirstOrDefault();
            if (user == null)
            {
                return false;
            }

            AppCredentials? credentials = _dataAccesor.ReadWhere<AppCredentials>(ac => user.Id == ac.UserId).FirstOrDefault();
            if (credentials != null && Security.Login(credentials, password))
            {
                User = user;
                return true;
            }

            return false;
        }

        public bool InitializeBot()
        {
            Notify("Initializing bot...");

            _botTrader = Services.GetRequiredService<IBotTraderService>();

            return _botTrader.InitializeAsync().Result;
        }

        public void ShutdownBot()
        {
            if (_botTrader != null)
            {
                Notify("Shutting down bot...");
                Task.Run(async () => await _botTrader.ShutdownAsync()).Wait();
            }
        }

        internal void Notify(Notification notification)
        {
            LastNotification = notification;
        }

        internal void Notify(string message)
        {
            Notify(new Notification()
            {
                NotificationType = Notification.NotificationTypes.Message,
                Message = message
            });
        }

        private ServiceCollection ConfigureServices(ILogger logger, bool useMockData)
        {
            ServiceCollection services = new();

            logger.LogDebug("Beginning to configure services for business application...");

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
                    getKeyFunction:() => { return User?.CoinAPIKey; }));

                services.AddSingleton<IExchangeService, BinanceExchangeService>(serviceProvider => new BinanceExchangeService(
                    logger: serviceProvider.GetRequiredService<ILogger>(),
                    loggerFactory: serviceProvider.GetRequiredService<ILoggerFactory>(),
                    httpClientFactory: serviceProvider.GetRequiredService<IHttpClientFactory>(),
                    getKeysFunction: () => { return (User?.BinanceKey, User?.BinanceSecretKey); }));

                //TODO: Obviously will need a real analyzer.
                services.AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>();
            }

            logger.LogDebug("... Bot service");
            services.AddSingleton<IBotTraderService, BitcoinBotTraderService>(serviceProvider => new BitcoinBotTraderService(
                cryptoDataService: serviceProvider.GetRequiredService<IMarketDataService>(),
                exchangeService: serviceProvider.GetRequiredService<IExchangeService>(),
                tradeAnalyzerService: serviceProvider.GetRequiredService<ITradeAnalyzerService>(),
                logger: serviceProvider.GetRequiredService<ILogger>(),
                notifyAction: Notify));

            return services;
        }

        public void UpdateUser()
        {
            if (this.User != null)
            {
                _dataAccesor.Update(this.User);
            }
        }
    }
}
