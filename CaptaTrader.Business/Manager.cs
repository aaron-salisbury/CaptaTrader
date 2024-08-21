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
    public class Manager
    {
        internal static readonly string COMPRESSION_CLIENT_NAME = "CompressionClient";

        public AppData AppData { get; set; }
        public BitcoinTrader? BitcoinTrader { get; set; }

        internal static IServiceProvider? Services { get; set; }

        private ILogger _logger;

        public Manager(ILogger logger)
        {
            _logger = logger;

            AppData = Load();
        }

        public IEnumerable<string?> InitializeBot(SecureString encryptPhrase)
        {
            if (Services == null)
            {
                yield return "Initializing services...";
                ConfigureServices(encryptPhrase);
                if (Services == null)
                {
                    _logger.LogError("Failed to initialize services. Exiting.");
                    yield return null;
                }
            }

            if (Services != null && BitcoinTrader == null)
            {
                yield return "Initializing bot...";
                BitcoinTrader = new BitcoinTrader(
                    Services.GetRequiredService<ICryptoDataService>(),
                    Services.GetRequiredService<IExchangeService>(),
                    _logger);
                BitcoinTrader.Initialize();
            }
        }

        public async Task<bool> SeekTradeAsync()
        {
            if (BitcoinTrader == null)
            {
                _logger.LogError("Bot must be initialized before seeking to trade.");
            }

            _logger.LogDebug("SeekTradeAsync()");
            await Task.Delay(5000);
            return true;
        }

        private void ConfigureServices(SecureString encryptPhrase)
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

            Services = services.BuildServiceProvider();
        }

        private AppData Load()
        {
            return Task.Run(() => CRUD.ReadDataAsync<AppData>(_logger)).Result 
                ?? new AppData();
        }

        public void Save()
        {
            Task.Run(() => CRUD.UpdateDataAsync(AppData, _logger));
        }
    }
}
