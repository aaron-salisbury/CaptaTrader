using CaptaTrader.Data.Database.Migrations;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Data.Database;

internal sealed class ApplicationDatabaseInitializer
{
    private static readonly string[] LEGACY_SIDE_FILE_SUFFIXES = ["-wal", "-shm", "-journal"];

    private readonly string _databasePath;
    private readonly IApplicationDatabase _database;
    private readonly IReadOnlyList<Migration> _migrations;

    internal ApplicationDatabaseInitializer(string databasePath, IApplicationDatabase database, IEnumerable<Migration> migrations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(migrations);

        _databasePath = databasePath;
        _database = database;
        _migrations = [.. migrations.OrderBy(migration => migration.Version)];

        if (_migrations.Select(migration => migration.Version).Distinct().Count() != _migrations.Count)
        {
            throw new InvalidOperationException("Database migration versions must be unique.");
        }
    }

    internal async Task<string?> InitializeAsync(CancellationToken cancellationToken = default)
    {
        string? legacyBackupPath = await SetAsideLegacyDatabaseAsync(cancellationToken);

        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        await CreateMigrationTableAsync(connection, cancellationToken);
        long currentVersion = await GetCurrentVersionAsync(connection, cancellationToken);

        foreach (Migration migration in _migrations.Where(migration => migration.Version > currentVersion))
        {
            await migration.ApplyAsync(connection, cancellationToken);
        }

        return legacyBackupPath;
    }

    /// <summary>
    /// The earlier, exploratory version of the app used the same file name for an unrelated schema
    /// (<c>EndUser</c>, <c>TradeOrder</c>, …) holding only mock-exchange data. It is not converted; it is
    /// set aside, so the new schema starts clean and nothing is lost.
    /// </summary>
    /// <remarks>
    /// SQLite's online backup copies the whole database, including anything still in a WAL or journal, into
    /// one self-contained file. The original is deleted only after the copy completes, so an interruption
    /// leaves the original in place and the next start simply backs it up again.
    /// </remarks>
    private async Task<string?> SetAsideLegacyDatabaseAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_databasePath) || !await IsLegacyDatabaseAsync(cancellationToken))
        {
            return null;
        }

        string directory = Path.GetDirectoryName(_databasePath)!;
        string backupPath = Path.Combine(directory, $"{Path.GetFileNameWithoutExtension(_databasePath)}.legacy-{DateTime.UtcNow:yyyyMMddHHmmss}.db");

        await using (SqliteConnection source = new(new SqliteConnectionStringBuilder { DataSource = _databasePath, Pooling = false }.ToString()))
        await using (SqliteConnection destination = new(new SqliteConnectionStringBuilder { DataSource = backupPath, Pooling = false }.ToString()))
        {
            await source.OpenAsync(cancellationToken);
            await destination.OpenAsync(cancellationToken);
            source.BackupDatabase(destination);
        }

        File.Delete(_databasePath);

        foreach (string suffix in LEGACY_SIDE_FILE_SUFFIXES)
        {
            File.Delete(_databasePath + suffix);
        }

        return backupPath;
    }

    private async Task<bool> IsLegacyDatabaseAsync(CancellationToken cancellationToken)
    {
        SqliteConnectionStringBuilder builder = new() { DataSource = _databasePath, Mode = SqliteOpenMode.ReadOnly, Pooling = false };
        await using SqliteConnection connection = new(builder.ToString());
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'EndUser')
                AND NOT EXISTS (SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = 'SchemaMigration');
            """;

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private static async Task CreateMigrationTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS SchemaMigration (
                Version INTEGER NOT NULL PRIMARY KEY,
                Description TEXT NOT NULL,
                AppliedAtUtc TEXT NOT NULL
            );
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    private static async Task<long> GetCurrentVersionAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COALESCE(MAX(Version), 0) FROM SchemaMigration;";

        return await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }
}
