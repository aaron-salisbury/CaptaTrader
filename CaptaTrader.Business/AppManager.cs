using CaptaTrader.Business.Base;
using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services;
using CaptaTrader.Data.Base.Extensions;
using CaptaTrader.Data.DataAccess;
using CaptaTrader.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
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

        private ApplicationDbContext _dbContext;
        private IBotTraderService? _botTrader;

        public AppManager(ILogger logger, bool useMockData)
        {
            Services = ConfigureServices(logger, useMockData).BuildServiceProvider();

            _dbContext = Services.GetRequiredService<ApplicationDbContext>();
        }

        public bool CreateUser(string userName, SecureString password)
        {
            if (string.IsNullOrEmpty(userName) || _dbContext.Users.Any(u => string.Equals(userName, u.UserName)))
            {
                return false;
            }

            User newUser = new() { UserName = userName };
            _dbContext.Users.Add(newUser);
            User = newUser;
            SaveUser();

            AppCredentials credentials = Data.Base.Security.CreateAppCredentials(password);
            credentials.UserId = newUser.UserId;

            _dbContext.AppCredentials.Add(credentials);
            Save();

            return true;
        }

        public bool VerifyUser(string userName, SecureString password)
        {
            User? user = _dbContext.Users.Where(u => string.Equals(userName, u.UserName)).FirstOrDefault();
            if (user == null)
            {
                return false;
            }

            AppCredentials? credentials = _dbContext.AppCredentials.Where(ac => user.UserId == ac.UserId).FirstOrDefault();

            if (credentials != null && Data.Base.Security.Login(credentials, password))
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

            //TODO: Verify service API keys are good and can receive from those services.
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
            services.AddDataAccess<ApplicationDbContext>();

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
                services.AddSingleton<ICryptoDataService, MockDataService>();
                services.AddSingleton<IExchangeService, MockExchangeService>();
                services.AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>();
            }
            else
            {
                services.AddSingleton<ICryptoDataService, CoinApiDataService>(serviceProvider => new CoinApiDataService(
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
                cryptoDataService: serviceProvider.GetRequiredService<ICryptoDataService>(),
                exchangeService: serviceProvider.GetRequiredService<IExchangeService>(),
                tradeAnalyzerService: serviceProvider.GetRequiredService<ITradeAnalyzerService>(),
                logger: serviceProvider.GetRequiredService<ILogger>(),
                notifyAction: Notify));

            return services;
        }

        public void SaveUser()
        {
            Save();

            if (User != null)
            {
                _dbContext.Entry(User).Reload();
            }
        }

        internal void Save()
        {
            _dbContext.SaveChanges();
        }
    }
}
