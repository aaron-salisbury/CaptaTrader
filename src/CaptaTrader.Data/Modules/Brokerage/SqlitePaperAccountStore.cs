using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Data.Database;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Data.Modules.Brokerage;

internal sealed class SqlitePaperAccountStore : IPaperAccountStore
{
    private readonly IApplicationDatabase _database;

    public SqlitePaperAccountStore(IApplicationDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<PaperAccount> GetOrCreateAsync(decimal startingCash, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO PaperAccount (PaperAccountId, CashUsd, Bitcoin)
            VALUES (1, @CashUsd, '0')
            ON CONFLICT(PaperAccountId) DO NOTHING;

            SELECT CashUsd, Bitcoin FROM PaperAccount WHERE PaperAccountId = 1;
            """;

        PaperAccountRow row = await connection.QuerySingleAsync<PaperAccountRow>(
            new CommandDefinition(sql, new { CashUsd = SqliteValues.ToText(startingCash) }, cancellationToken: cancellationToken));

        return new PaperAccount(SqliteValues.ToDecimal(row.CashUsd), SqliteValues.ToDecimal(row.Bitcoin));
    }

    public async Task SaveAsync(PaperAccount account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO PaperAccount (PaperAccountId, CashUsd, Bitcoin)
            VALUES (1, @CashUsd, @Bitcoin)
            ON CONFLICT(PaperAccountId) DO UPDATE SET CashUsd = excluded.CashUsd, Bitcoin = excluded.Bitcoin;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { CashUsd = SqliteValues.ToText(account.CashUsd), Bitcoin = SqliteValues.ToText(account.Bitcoin) },
            cancellationToken: cancellationToken));
    }

    private sealed class PaperAccountRow
    {
        public string CashUsd { get; set; } = string.Empty;
        public string Bitcoin { get; set; } = string.Empty;
    }
}
