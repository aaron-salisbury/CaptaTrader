using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.Settings;
using CaptaTrader.Business.Modules.UserAccess;
using CaptaTrader.Data.Database;
using CaptaTrader.Data.Database.Migrations;
using CaptaTrader.Data.Modules.Brokerage;
using CaptaTrader.Data.Modules.Ledger;
using CaptaTrader.Data.Modules.Settings;
using CaptaTrader.Data.Modules.UserAccess;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace CaptaTrader.Data;

public static class DependencyInjection
{
    public const string DATABASE_FILE_NAME = "captatrader.db";

    /// <summary>
    /// Registers internal data-tier services.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection RegisterInternalDataServices(this IServiceCollection services, string applicationDataDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDataDirectory);

        if (!Directory.Exists(applicationDataDirectory))
        {
            throw new DirectoryNotFoundException($"The application data directory '{applicationDataDirectory}' does not exist.");
        }

        string databasePath = Path.Combine(applicationDataDirectory, DATABASE_FILE_NAME);

        services.AddSingleton<IApplicationDatabase>(_ => new ApplicationDatabase(databasePath));
        services.AddSingleton<Migration, CreateInitialSchemaMigration>();
        services.AddSingleton(serviceProvider => new ApplicationDatabaseInitializer(
            databasePath,
            serviceProvider.GetRequiredService<IApplicationDatabase>(),
            serviceProvider.GetServices<Migration>()));

        // Stores open a connection per call and hold no state, so one instance serves the whole app.
        services.AddSingleton<IApplicationSettingStore, SqliteApplicationSettingStore>();
        services.AddSingleton<IUserAccountStore, SqliteUserAccountStore>();
        services.AddSingleton<ILedgerStore, SqliteLedgerStore>();
        services.AddSingleton<IPaperAccountStore, SqlitePaperAccountStore>();

        return services;
    }
}
