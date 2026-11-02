using Dapper;
using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Data.Database.Migrations;

/// <remarks>
/// Monetary amounts and BTC quantities are stored as invariant-culture decimal TEXT so no precision is
/// lost to SQLite's floating-point REAL. Instants are stored as ISO 8601 UTC TEXT, which sorts correctly.
/// </remarks>
internal sealed class CreateInitialSchemaMigration : Migration
{
    internal override long Version => 1;
    internal override string Description => "Create login, settings, ledger, tax lot, and paper account storage";

    protected override Task ExecuteAsync(SqliteConnection connection, SqliteTransaction transaction, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE ApplicationSetting (
                Key TEXT NOT NULL PRIMARY KEY,
                Value TEXT NOT NULL
            );

            CREATE TABLE UserAccount (
                UserAccountId INTEGER NOT NULL PRIMARY KEY CHECK (UserAccountId = 1),
                UserName TEXT NOT NULL,
                PasswordHash BLOB NOT NULL,
                PasswordSalt BLOB NOT NULL,
                PasswordIterations INTEGER NOT NULL,
                CreatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE OrderAttempt (
                OrderAttemptId TEXT NOT NULL PRIMARY KEY,
                BrokerageId TEXT NOT NULL,
                StrategyId TEXT NOT NULL,
                Side TEXT NOT NULL CHECK (Side IN ('Buy', 'Sell')),
                RequestedQuantity TEXT NULL,
                RequestedNotional TEXT NULL,
                ReferencePrice TEXT NOT NULL,
                Reason TEXT NOT NULL,
                AttemptedAtUtc TEXT NOT NULL,
                Status TEXT NOT NULL,
                BrokerOrderId TEXT NULL,
                FilledQuantity TEXT NULL,
                AveragePrice TEXT NULL,
                Fee TEXT NULL,
                FeeIsEstimated INTEGER NOT NULL CHECK (FeeIsEstimated IN (0, 1)),
                CompletedAtUtc TEXT NULL,
                Message TEXT NULL
            );
            CREATE INDEX IX_OrderAttempt_AttemptedAtUtc ON OrderAttempt (AttemptedAtUtc);
            CREATE INDEX IX_OrderAttempt_Status ON OrderAttempt (Status);

            CREATE TABLE TaxLot (
                TaxLotId TEXT NOT NULL PRIMARY KEY,
                BrokerageId TEXT NOT NULL,
                AcquiredAtUtc TEXT NOT NULL,
                Quantity TEXT NOT NULL,
                CostBasis TEXT NOT NULL,
                RemainingQuantity TEXT NOT NULL,
                RemainingCostBasis TEXT NOT NULL,
                IsOpen INTEGER NOT NULL CHECK (IsOpen IN (0, 1)),
                SourceOrderAttemptId TEXT NOT NULL REFERENCES OrderAttempt (OrderAttemptId)
            );
            CREATE INDEX IX_TaxLot_Open ON TaxLot (BrokerageId, IsOpen, AcquiredAtUtc);
            CREATE UNIQUE INDEX UX_TaxLot_SourceOrderAttemptId ON TaxLot (SourceOrderAttemptId);

            CREATE TABLE LotDisposal (
                LotDisposalId TEXT NOT NULL PRIMARY KEY,
                TaxLotId TEXT NOT NULL REFERENCES TaxLot (TaxLotId),
                BrokerageId TEXT NOT NULL,
                AcquiredAtUtc TEXT NOT NULL,
                DisposedAtUtc TEXT NOT NULL,
                Quantity TEXT NOT NULL,
                Proceeds TEXT NOT NULL,
                CostBasis TEXT NOT NULL,
                SellOrderAttemptId TEXT NOT NULL REFERENCES OrderAttempt (OrderAttemptId)
            );
            CREATE INDEX IX_LotDisposal_DisposedAtUtc ON LotDisposal (DisposedAtUtc);

            CREATE TABLE PaperAccount (
                PaperAccountId INTEGER NOT NULL PRIMARY KEY CHECK (PaperAccountId = 1),
                CashUsd TEXT NOT NULL,
                Bitcoin TEXT NOT NULL
            );
            """;

        return connection.ExecuteAsync(new CommandDefinition(sql, transaction: transaction, cancellationToken: cancellationToken));
    }
}
