using CaptaTrader.Business.DTOs;
using CaptaTrader.Business.Services;
using CaptaTrader.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CaptaTrader.Business
{
    public class Manager
    {
        public AppData AppData { get; set; }
        public ITradingService? TradingBot { get; set; }

        internal static IServiceProvider? Services { get; set; }

        private ILogger _logger;

        public Manager(ILogger logger)
        {
            _logger = logger;

            AppData = Load();
        }

        public void LaunchBot(string passPhrase)
        {
            _logger.LogInformation("Initializing services.");
            ConfigureServices(passPhrase);
            if (Services == null)
            {
                _logger.LogError("Failed to initialize services. Exiting.");
                return;
            }

            _logger.LogInformation("Initializing bot.");
            TradingBot = Services.GetRequiredService<ITradingService>();
            if (TradingBot == null)
            {
                _logger.LogError("Failed to initialize bot. Exiting.");
                return;
            }
            TradingBot.Initialize();
            //TODO: Make fancier using spectre console.
            _logger.LogInformation(TradingBot.ToString());
        }

        private void ConfigureServices(string passPhrase)
        {
            ServiceCollection services = new();

            services.AddSingleton(typeof(ILogger), _logger);
            services.AddHttpClient();

            var valutService = services.AddSingleton<IKeyVaultService, KeyVaultService>(serviceProvider => new KeyVaultService(
                logger: serviceProvider.GetRequiredService<ILogger>(),
                appData: AppData,
                passPhrase: passPhrase));

            services.AddSingleton<ICryptoDataService, NomicsCryptoDataService>(); //TODO: Nomics no longer exists. Need to write a new ICryptoDataService.
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
