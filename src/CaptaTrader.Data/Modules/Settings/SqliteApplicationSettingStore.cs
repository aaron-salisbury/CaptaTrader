using CaptaTrader.Business.Modules.Settings;
using CaptaTrader.Data.Database;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Data.Modules.Settings;

internal sealed class SqliteApplicationSettingStore : IApplicationSettingStore
{
    private readonly IApplicationDatabase _database;

    public SqliteApplicationSettingStore(IApplicationDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = "SELECT Value FROM ApplicationSetting WHERE Key = @Key;";
        return await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(sql, new { Key = key }, cancellationToken: cancellationToken));
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO ApplicationSetting (Key, Value)
            VALUES (@Key, @Value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, new { Key = key, Value = value }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = "DELETE FROM ApplicationSetting WHERE Key = @Key;";
        await connection.ExecuteAsync(new CommandDefinition(sql, new { Key = key }, cancellationToken: cancellationToken));
    }
}
