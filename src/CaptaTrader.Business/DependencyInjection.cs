using CaptaTrader.Business.Modules.AutoTrading;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Business.Modules.Settings;
using CaptaTrader.Business.Modules.Strategies;
using CaptaTrader.Business.Modules.TaxReporting;
using CaptaTrader.Business.Modules.UserAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace CaptaTrader.Business;

public static class DependencyInjection
{
    /// <summary>
    /// Registers internal business-tier services.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection RegisterInternalBusinessServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<UserSession>();
        services.AddSingleton<ProviderCatalog>();
        services.AddSingleton<ITradingStrategy, RandomVolatilityStrategy>();
        services.AddSingleton<StrategyCatalog>();
        services.AddSingleton<ManageLogin>();
        services.AddSingleton<ManageTradingSettings>();
        services.AddSingleton<ConfigureProviders>();
        services.AddSingleton<BuildTaxReports>();
        services.AddSingleton<AutoTrader>();

        return services;
    }
}
