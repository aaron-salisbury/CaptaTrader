using Microsoft.Data.Sqlite;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Data.Database;

internal sealed class ApplicationDatabase : IApplicationDatabase
{
    private const int BUSY_TIMEOUT_MILLISECONDS = 5_000;

    private readonly string _connectionString;

    internal ApplicationDatabase(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        SqliteConnectionStringBuilder connectionStringBuilder = new()
        {
            DataSource = databasePath,
            ForeignKeys = true
        };

        _connectionString = connectionStringBuilder.ToString();
    }

    public async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        SqliteConnection connection = new(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            await ConfigureAsync(connection, cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Write-ahead logging lets the menu read the ledger while the trading session writes to it, and
    /// the busy timeout waits briefly for a lock instead of failing.
    /// </summary>
    private static async Task ConfigureAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA journal_mode = WAL; PRAGMA busy_timeout = {BUSY_TIMEOUT_MILLISECONDS};";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
