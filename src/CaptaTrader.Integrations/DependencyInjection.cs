using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Credentials;
using CaptaTrader.Business.Modules.MarketData;
using CaptaTrader.Business.Modules.TaxReporting;
using CaptaTrader.Integrations.Brokerage.Alpaca;
using CaptaTrader.Integrations.Brokerage.Paper;
using CaptaTrader.Integrations.Credentials;
using CaptaTrader.Integrations.MarketData.Coinbase;
using CaptaTrader.Integrations.MarketData.Replay;
using CaptaTrader.Integrations.TaxReporting;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace CaptaTrader.Integrations;

public static class DependencyInjection
{
    public const string TAX_REPORT_DIRECTORY_NAME = "tax-reports";

    /// <summary>
    /// Registers internal integrations-tier services. Registration order is presentation order, and the
    /// first Provider of each kind is the default: paper trading against live prices.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection RegisterInternalIntegrationsServices(this IServiceCollection services, string applicationDataDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDataDirectory);

        services.AddSingleton<IBrokerageProvider, PaperBrokerageProvider>();
        services.AddSingleton<IBrokerageProvider, AlpacaPaperBrokerageProvider>();

        services.AddSingleton<IMarketDataProvider, CoinbaseMarketDataProvider>();
        services.AddSingleton<IMarketDataProvider, HistoricalReplayMarketDataProvider>();

        services.AddSingleton<ITaxReportWriter>(new CsvTaxReportWriter(Path.Combine(applicationDataDirectory, TAX_REPORT_DIRECTORY_NAME)));

        services.AddPlatformCredentialStore();

        return services;
    }

    private static IServiceCollection AddPlatformCredentialStore(this IServiceCollection services)
    {
        services.AddSingleton<ICredentialStore>(_ =>
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new WindowsCredentialStore();
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxSecretServiceCredentialStore();
            }

            return new UnsupportedCredentialStore();
        });

        return services;
    }
}
