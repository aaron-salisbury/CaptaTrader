using CaptaTrader.Business.Modules.UserAccess;
using CaptaTrader.Data.Database;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Data.Modules.UserAccess;

internal sealed class SqliteUserAccountStore : IUserAccountStore
{
    private readonly IApplicationDatabase _database;

    public SqliteUserAccountStore(IApplicationDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task<UserAccount?> GetAsync(CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT UserName, PasswordHash, PasswordSalt, PasswordIterations, CreatedAtUtc
            FROM UserAccount
            WHERE UserAccountId = 1;
            """;

        UserAccountRow? row = await connection.QuerySingleOrDefaultAsync<UserAccountRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        return new UserAccount(
            row.UserName,
            new PasswordHash(row.PasswordHash, row.PasswordSalt, (int)row.PasswordIterations),
            SqliteValues.ToInstant(row.CreatedAtUtc));
    }

    public async Task<bool> TryCreateAsync(UserAccount account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        // The single-row CHECK makes "one login per installation" a data-integrity guarantee as well.
        const string sql = """
            INSERT INTO UserAccount (UserAccountId, UserName, PasswordHash, PasswordSalt, PasswordIterations, CreatedAtUtc)
            VALUES (1, @UserName, @PasswordHash, @PasswordSalt, @PasswordIterations, @CreatedAtUtc)
            ON CONFLICT(UserAccountId) DO NOTHING;
            """;

        int inserted = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                account.UserName,
                PasswordHash = account.Password.Hash,
                PasswordSalt = account.Password.Salt,
                PasswordIterations = account.Password.Iterations,
                CreatedAtUtc = SqliteValues.ToText(account.CreatedAtUtc)
            },
            cancellationToken: cancellationToken));

        return inserted == 1;
    }

    private sealed class UserAccountRow
    {
        public string UserName { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; } = [];
        public byte[] PasswordSalt { get; set; } = [];
        public long PasswordIterations { get; set; }
        public string CreatedAtUtc { get; set; } = string.Empty;
    }
}
