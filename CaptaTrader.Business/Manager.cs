using CaptaTrader.Business.DTOs;
using CaptaTrader.Business.Services;
using CaptaTrader.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Security;

namespace CaptaTrader.Business
{
    public class Manager
    {
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
            if (Services == null)
            {
                _logger.LogInformation("Initializing services.");
                ConfigureServices(encryptPhrase);
                if (Services == null)
                {
                    _logger.LogError("Failed to initialize services. Exiting.");
                    return;
                }
            }

            if (_tradingBot == null)
            {
                _logger.LogInformation("Initializing bot.");
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
            services.AddHttpClient();

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
