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

        public AppData AppData { get; set; }
        public BitcoinTrader? BitcoinTrader { get; set; }

        private Notification? _lastNotification;
        public Notification? LastNotification
        {
            get => _lastNotification;
            set => SetProperty(ref _lastNotification, value);
        }

        internal static IServiceProvider? Services { get; set; }

        private ILogger _logger;

        public Manager(ILogger logger)
        {
            _logger = logger;

            AppData = Load();
        }

        public bool InitializeBot(SecureString encryptPhrase)
        {
            Notify("Initializing services...");
            Services = ConfigureServices(encryptPhrase).BuildServiceProvider();

            Notify("Initializing bot...");
            BitcoinTrader = new BitcoinTrader(
                Services.GetRequiredService<ICryptoDataService>(),
                Services.GetRequiredService<IExchangeService>(),
                _logger,
                Notify);

            return BitcoinTrader.InitializeAsync().Result;
        }

        public void ShutdownBot()
        {
            if (BitcoinTrader != null)
            {
                Notify("Shutting down bot...");
                Task.Run(async () => await BitcoinTrader.ShutdownAsync());
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

            var valutService = services.AddSingleton<IKeyVaultService, KeyVaultService>(serviceProvider => new KeyVaultService(
                logger: serviceProvider.GetRequiredService<ILogger>(),
                appData: AppData,
                encryptPhrase: encryptPhrase));

            services.AddSingleton<ICryptoDataService, CoinApiDataService>();
            services.AddSingleton<IExchangeService, BinanceExchangeService>();

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
