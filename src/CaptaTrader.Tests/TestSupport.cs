using CaptaTrader.Business;
using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Credentials;
using CaptaTrader.Business.Modules.MarketData;
using CaptaTrader.Business.Modules.TaxReporting;
using CaptaTrader.Data;
using CaptaTrader.Data.Database;
using CaptaTrader.Integrations.Brokerage.Paper;
using CaptaTrader.Integrations.MarketData.Replay;
using CaptaTrader.Integrations.TaxReporting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Tests;

/// <summary>
/// A unique temporary directory, removed (with SQLite pools released) when disposed.
/// </summary>
internal sealed class TestDirectory : IDisposable
{
    internal TestDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"CaptaTrader.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    internal string Path { get; }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}

internal sealed class InMemoryCredentialStore : ICredentialStore
{
    private readonly ConcurrentDictionary<string, CredentialMaterial> _values = new();

    internal int Count => _values.Count;

    public Task DeleteAsync(CredentialReference reference, CancellationToken cancellationToken = default)
    {
        _values.TryRemove(reference.Value, out _);
        return Task.CompletedTask;
    }

    public Task<CredentialMaterial?> GetAsync(CredentialReference reference, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_values.TryGetValue(reference.Value, out CredentialMaterial? material) ? material : null);
    }

    public Task StoreAsync(CredentialReference reference, CredentialMaterial material, CancellationToken cancellationToken = default)
    {
        _values[reference.Value] = material;
        return Task.CompletedTask;
    }
}

/// <summary>
/// System time with a chosen local time zone, so tax-year boundaries can be tested deterministically.
/// </summary>
internal sealed class ZonedTimeProvider : TimeProvider
{
    private readonly TimeZoneInfo _zone;

    internal ZonedTimeProvider(TimeZoneInfo zone)
    {
        _zone = zone;
    }

    public override TimeZoneInfo LocalTimeZone => _zone;
}

internal static class TestServices
{
    /// <summary>
    /// Real Data and Business tiers over a temporary SQLite file, with the simulated Providers running
    /// at full speed and an in-memory credential store.
    /// </summary>
    internal static async Task<ServiceProvider> CreateAsync(string directory, Action<IServiceCollection>? configure = null)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.RegisterInternalDataServices(directory);
        services.AddSingleton<IBrokerageProvider>(provider => new PaperBrokerageProvider(provider.GetRequiredService<IPaperAccountStore>(), TimeSpan.Zero));
        services.AddSingleton<IMarketDataProvider>(new HistoricalReplayMarketDataProvider { TickInterval = TimeSpan.Zero });
        services.AddSingleton<ICredentialStore, InMemoryCredentialStore>();
        services.AddSingleton<ITaxReportWriter>(new CsvTaxReportWriter(System.IO.Path.Combine(directory, "tax-reports")));
        configure?.Invoke(services);
        services.RegisterInternalBusinessServices();

        ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        await provider.InitializeApplicationDataAsync();

        return provider;
    }
}
