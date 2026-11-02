using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Data.Database;

public static class ApplicationDataInitialization
{
    /// <summary>Creates or upgrades the database.</summary>
    /// <returns>
    /// The path the database from the earlier, unrelated version of the app was moved to, or null when there
    /// was none.
    /// </returns>
    public static Task<string?> InitializeApplicationDataAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return serviceProvider
            .GetRequiredService<ApplicationDatabaseInitializer>()
            .InitializeAsync(cancellationToken);
    }
}
