using CaptaTrader.Business.DTOs;
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

        internal static IServiceProvider? Services { get; set; }

        private ILogger _logger;
        public ITradingService? _tradingBot;

        public Manager(ILogger logger)
        {
            _logger = logger;

            AppData = Load();
        }

        public void InitializeBot(SecureString encryptPhrase)
        {
            try
            {
                if (Services == null)
                {
                    _logger.LogInformation("Initializing services...");
                    ConfigureServices(encryptPhrase);
                    if (Services == null)
                    {
                        _logger.LogError("Failed to initialize services. Exiting.");
                        return;
                    }
                }

                if (_tradingBot == null)
                {
                    _logger.LogInformation("Initializing bot...");
                    _tradingBot = Services.GetRequiredService<ITradingService>();
                    if (_tradingBot == null)
                    {
                        _logger.LogError("Failed to initialize bot. Exiting.");
                        return;
                    }
                    _tradingBot.Initialize();
                    //TODO: Make fancier using spectre console. Maybe yield return messages.
                    _logger.LogInformation(_tradingBot.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(default, ex, "Failed to initialize services.");
            }
        }

        public async Task SeekTradeAsync()
        {
            if (_tradingBot == null)
            {
                _logger.LogError("Bot must be initialized before seeking to trade.");
            }

            _logger.LogDebug("SeekTradeAsync()");
            await Task.Delay(5000);
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
            services.AddSingleton<ITradingService, BitcoinTradingService>();

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
