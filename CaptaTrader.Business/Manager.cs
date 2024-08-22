// Ignore Spelling: Bitcoin App

using CaptaTrader.Business.Base;
using CaptaTrader.Business.DTOs;
using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services;
using CaptaTrader.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Security;

namespace CaptaTrader.Business
{
    public class Manager : ObservableModel
    {
        internal static readonly string COMPRESSION_CLIENT_NAME = "CompressionClient";

        public bool UseMockData { get; private set; }
        public AppData AppData { get; set; }

        private Notification? _lastNotification;
        public Notification? LastNotification
        {
            get => _lastNotification;
            set => SetProperty(ref _lastNotification, value);
        }

        internal static IServiceProvider? Services { get; set; }

        private ILogger _logger;
        private IBotTraderService? _botTrader;

        public Manager(ILogger logger, bool useMockData = false)
        {
            _logger = logger;

            UseMockData = useMockData;
            AppData = Load();
        }

        public bool InitializeBot(SecureString encryptPhrase)
        {
            Notify("Initializing services...");
            Services = ConfigureServices(encryptPhrase).BuildServiceProvider();

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

        private ServiceCollection ConfigureServices(SecureString encryptPhrase)
        {
            ServiceCollection services = new();

            services.AddSingleton(typeof(ILogger), _logger);

            services.AddHttpClient(COMPRESSION_CLIENT_NAME, c =>
            {
                c.DefaultRequestHeaders.Add("Accept-Encoding", "deflate, gzip");

            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
            });

            services.AddSingleton<IKeyVaultService, KeyVaultService>(serviceProvider => new KeyVaultService(
                logger: serviceProvider.GetRequiredService<ILogger>(),
                appData: AppData,
                encryptPhrase: encryptPhrase));

            if (UseMockData)
            {
                services.AddSingleton<ICryptoDataService, MockDataService>();
                services.AddSingleton<IExchangeService, MockExchangeService>();
                services.AddSingleton<ITradeAnalyzerService, MockTradeAnalyzerService>();
            }
            else
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

        private AppData Load()
        {
            return Task.Run(() => CRUD.ReadDataAsync<AppData>(_logger)).Result ?? new AppData();
        }

        public void Save()
        {
            Task.Run(() => CRUD.UpdateDataAsync(AppData, _logger));
        }
    }
}
