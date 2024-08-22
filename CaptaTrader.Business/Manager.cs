// Ignore Spelling: Bitcoin App

using CaptaTrader.Business.Base;
using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services;
using CaptaTrader.Data;
using CaptaTrader.Data.Base;
using CaptaTrader.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security;
using System.Text;

namespace CaptaTrader.Business
{
    public class Manager : ObservableModel
    {
        internal static readonly string COMPRESSION_CLIENT_NAME = "CompressionClient";

        public bool UseMockData { get; private set; }
        public IServiceProvider Services { get; private set; }
        public User? User { get; private set; }

        private Notification? _lastNotification;
        public Notification? LastNotification
        {
            get => _lastNotification;
            set => SetProperty(ref _lastNotification, value);
        }

        private DbAccess _dbAccess;
        private IBotTraderService? _botTrader;

        public Manager(ILogger logger, bool useMockData = false)
        {
            Services = ConfigureServices(logger).BuildServiceProvider();

            _dbAccess = Services.GetRequiredService<DbAccess>();
            User = _dbAccess.DbContext.Users.FirstOrDefault();

            UseMockData = useMockData;
        }

        public bool UserExists() => _dbAccess.DbContext.Users.Any();

        public async Task CreateUserAsync(SecureString password)
        {
            User = Data.Base.Authorization.CreateAuthorizedUser(Encoding.ASCII.GetBytes(password.ToPlainString()));

            await _dbAccess.DbContext.Users.AddAsync(User);
            await _dbAccess.DbContext.SaveChangesAsync();
        }

        public bool VerifyUser(SecureString password)
        {
            byte[] passwordBytes = Encoding.ASCII.GetBytes(password.ToPlainString());

            return User != null && Data.Base.Authorization.Login(User, passwordBytes);
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
                Task.Run(async () => await _botTrader.ShutdownAsync());
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

        private ServiceCollection ConfigureServices(ILogger logger)
        {
            ServiceCollection services = new();

            services.AddDatabaseAccess();

            services.AddSingleton(typeof(ILogger), logger);

            services.AddHttpClient(COMPRESSION_CLIENT_NAME, c =>
            {
                c.DefaultRequestHeaders.Add("Accept-Encoding", "deflate, gzip");

            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            });

            if (UseMockData)
            {
                services.AddSingleton<ICryptoDataService, MockDataService>();
                services.AddSingleton<IExchangeService, MockExchangeService>();
                services.AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>();
            }
            else //TODO: CoinApi and Binance need keys passed in, but what if the first user hasn't been created yet?
            {
                services.AddSingleton<ICryptoDataService, CoinApiDataService>();
                services.AddSingleton<IExchangeService, BinanceExchangeService>();
                //TODO: Obviously will need a real analyzer.
                services.AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>();
            }

            services.AddSingleton<IBotTraderService, BitcoinBotTraderService>(serviceProvider => new BitcoinBotTraderService(
                cryptoDataService: serviceProvider.GetRequiredService<ICryptoDataService>(),
                exchangeService: serviceProvider.GetRequiredService<IExchangeService>(),
                tradeAnalyzerService: serviceProvider.GetRequiredService<ITradeAnalyzerService>(),
                logger: serviceProvider.GetRequiredService<ILogger>(),
                notifyAction: Notify));

            return services;
        }

        public void Save()
        {
            Task.Run(() => _dbAccess.DbContext.SaveChangesAsync());
        }
    }
}
