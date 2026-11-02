using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Data.Database;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Data.Modules.Ledger;

internal sealed class SqliteLedgerStore : ILedgerStore
{
    private const string ATTEMPT_COLUMNS = """
        OrderAttemptId, BrokerageId, StrategyId, Side, RequestedQuantity, RequestedNotional, ReferencePrice, Reason,
        AttemptedAtUtc, Status, BrokerOrderId, FilledQuantity, AveragePrice, Fee, FeeIsEstimated, CompletedAtUtc, Message
        """;

    private const string LOT_COLUMNS = """
        TaxLotId, BrokerageId, AcquiredAtUtc, Quantity, CostBasis, RemainingQuantity, RemainingCostBasis, SourceOrderAttemptId
        """;

    private readonly IApplicationDatabase _database;

    public SqliteLedgerStore(IApplicationDatabase database)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
    }

    public async Task InsertAttemptAsync(OrderAttempt attempt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = $"""
            INSERT INTO OrderAttempt ({ATTEMPT_COLUMNS})
            VALUES (@OrderAttemptId, @BrokerageId, @StrategyId, @Side, @RequestedQuantity, @RequestedNotional, @ReferencePrice, @Reason,
                    @AttemptedAtUtc, @Status, @BrokerOrderId, @FilledQuantity, @AveragePrice, @Fee, @FeeIsEstimated, @CompletedAtUtc, @Message);
            """;

        await connection.ExecuteAsync(new CommandDefinition(sql, OrderAttemptRow.From(attempt), cancellationToken: cancellationToken));
    }

    public async Task CompleteAttemptAsync(OrderAttempt attempt, LedgerChanges changes, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(changes);
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);
        await using SqliteTransaction transaction = connection.BeginTransaction();

        const string updateAttemptSql = """
            UPDATE OrderAttempt
            SET Status = @Status, BrokerOrderId = @BrokerOrderId, FilledQuantity = @FilledQuantity, AveragePrice = @AveragePrice,
                Fee = @Fee, FeeIsEstimated = @FeeIsEstimated, CompletedAtUtc = @CompletedAtUtc, Message = @Message
            WHERE OrderAttemptId = @OrderAttemptId AND Status IN ('Pending', 'Unknown');
            """;

        const string insertLotSql = $"""
            INSERT INTO TaxLot ({LOT_COLUMNS}, IsOpen)
            VALUES (@TaxLotId, @BrokerageId, @AcquiredAtUtc, @Quantity, @CostBasis, @RemainingQuantity, @RemainingCostBasis, @SourceOrderAttemptId, @IsOpen);
            """;

        const string updateLotSql = """
            UPDATE TaxLot
            SET RemainingQuantity = @RemainingQuantity, RemainingCostBasis = @RemainingCostBasis, IsOpen = @IsOpen
            WHERE TaxLotId = @TaxLotId;
            """;

        const string insertDisposalSql = """
            INSERT INTO LotDisposal (LotDisposalId, TaxLotId, BrokerageId, AcquiredAtUtc, DisposedAtUtc, Quantity, Proceeds, CostBasis, SellOrderAttemptId)
            VALUES (@LotDisposalId, @TaxLotId, @BrokerageId, @AcquiredAtUtc, @DisposedAtUtc, @Quantity, @Proceeds, @CostBasis, @SellOrderAttemptId);
            """;

        try
        {
            int updated = await connection.ExecuteAsync(new CommandDefinition(updateAttemptSql, OrderAttemptRow.From(attempt), transaction, cancellationToken: cancellationToken));

            if (updated != 1)
            {
                // Only an open attempt can be completed, so a second completion (for example, two
                // processes reconciling the same order) cannot record the same fill twice.
                throw new InvalidOperationException($"Order attempt {attempt.Id} is not on the ledger or was already completed.");
            }

            if (changes.NewLot is not null)
            {
                await connection.ExecuteAsync(new CommandDefinition(insertLotSql, TaxLotRow.From(changes.NewLot), transaction, cancellationToken: cancellationToken));
            }

            foreach (TaxLot lot in changes.UpdatedLots)
            {
                await connection.ExecuteAsync(new CommandDefinition(updateLotSql, TaxLotRow.From(lot), transaction, cancellationToken: cancellationToken));
            }

            foreach (LotDisposal disposal in changes.Disposals)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    insertDisposalSql,
                    new
                    {
                        LotDisposalId = SqliteValues.ToText(disposal.Id),
                        TaxLotId = SqliteValues.ToText(disposal.LotId),
                        disposal.BrokerageId,
                        AcquiredAtUtc = SqliteValues.ToText(disposal.AcquiredAtUtc),
                        DisposedAtUtc = SqliteValues.ToText(disposal.DisposedAtUtc),
                        Quantity = SqliteValues.ToText(disposal.Quantity),
                        Proceeds = SqliteValues.ToText(disposal.Proceeds),
                        CostBasis = SqliteValues.ToText(disposal.CostBasis),
                        SellOrderAttemptId = SqliteValues.ToText(disposal.SellAttemptId)
                    },
                    transaction,
                    cancellationToken: cancellationToken));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<int> MarkAbandonedPendingAsUnknownAsync(string message, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = """
            UPDATE OrderAttempt
            SET Status = 'Unknown', Message = @Message
            WHERE Status = 'Pending';
            """;

        return await connection.ExecuteAsync(new CommandDefinition(sql, new { Message = message }, cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<OrderAttempt>> GetUnknownAttemptsAsync(string brokerageId, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = $"""
            SELECT {ATTEMPT_COLUMNS}
            FROM OrderAttempt
            WHERE Status = 'Unknown' AND BrokerageId = @BrokerageId
            ORDER BY AttemptedAtUtc;
            """;

        IEnumerable<OrderAttemptRow> rows = await connection.QueryAsync<OrderAttemptRow>(
            new CommandDefinition(sql, new { BrokerageId = brokerageId }, cancellationToken: cancellationToken));

        return [.. rows.Select(row => row.ToAttempt())];
    }

    public async Task<IReadOnlyList<DateTimeOffset>> GetAttemptTimesSinceAsync(string brokerageId, DateTimeOffset sinceUtc, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT AttemptedAtUtc
            FROM OrderAttempt
            WHERE BrokerageId = @BrokerageId AND AttemptedAtUtc >= @SinceUtc
            ORDER BY AttemptedAtUtc;
            """;

        IEnumerable<string> times = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            new { BrokerageId = brokerageId, SinceUtc = SqliteValues.ToText(sinceUtc) },
            cancellationToken: cancellationToken));

        return [.. times.Select(SqliteValues.ToInstant)];
    }

    public async Task<IReadOnlyList<OrderAttempt>> GetRecentAttemptsAsync(int count, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = $"""
            SELECT {ATTEMPT_COLUMNS}
            FROM OrderAttempt
            ORDER BY AttemptedAtUtc DESC
            LIMIT @Count;
            """;

        IEnumerable<OrderAttemptRow> rows = await connection.QueryAsync<OrderAttemptRow>(
            new CommandDefinition(sql, new { Count = count }, cancellationToken: cancellationToken));

        return [.. rows.Select(row => row.ToAttempt())];
    }

    public async Task<IReadOnlyList<OrderAttempt>> GetFilledAttemptsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = $"""
            SELECT {ATTEMPT_COLUMNS}
            FROM OrderAttempt
            WHERE Status = 'Filled' AND CompletedAtUtc >= @FromUtc AND CompletedAtUtc < @ToUtc
            ORDER BY CompletedAtUtc;
            """;

        IEnumerable<OrderAttemptRow> rows = await connection.QueryAsync<OrderAttemptRow>(new CommandDefinition(
            sql,
            new { FromUtc = SqliteValues.ToText(fromUtc), ToUtc = SqliteValues.ToText(toUtc) },
            cancellationToken: cancellationToken));

        return [.. rows.Select(row => row.ToAttempt())];
    }

    public async Task<IReadOnlyList<TaxLot>> GetOpenLotsAsync(string brokerageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(brokerageId);
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = $"""
            SELECT {LOT_COLUMNS}
            FROM TaxLot
            WHERE BrokerageId = @BrokerageId AND IsOpen = 1
            ORDER BY AcquiredAtUtc, TaxLotId;
            """;

        IEnumerable<TaxLotRow> rows = await connection.QueryAsync<TaxLotRow>(
            new CommandDefinition(sql, new { BrokerageId = brokerageId }, cancellationToken: cancellationToken));

        return [.. rows.Select(row => row.ToLot())];
    }

    public async Task<IReadOnlyList<LotDisposal>> GetDisposalsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT LotDisposalId, TaxLotId, BrokerageId, AcquiredAtUtc, DisposedAtUtc, Quantity, Proceeds, CostBasis, SellOrderAttemptId
            FROM LotDisposal
            WHERE DisposedAtUtc >= @FromUtc AND DisposedAtUtc < @ToUtc
            ORDER BY DisposedAtUtc, AcquiredAtUtc;
            """;

        IEnumerable<LotDisposalRow> rows = await connection.QueryAsync<LotDisposalRow>(new CommandDefinition(
            sql,
            new { FromUtc = SqliteValues.ToText(fromUtc), ToUtc = SqliteValues.ToText(toUtc) },
            cancellationToken: cancellationToken));

        return [.. rows.Select(row => row.ToDisposal())];
    }

    public async Task<(DateTimeOffset First, DateTimeOffset Last)?> GetFillRangeAsync(CancellationToken cancellationToken = default)
    {
        await using SqliteConnection connection = await _database.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT MIN(CompletedAtUtc) AS First, MAX(CompletedAtUtc) AS Last
            FROM OrderAttempt
            WHERE Status = 'Filled';
            """;

        (string? First, string? Last) range = await connection.QuerySingleAsync<(string? First, string? Last)>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        if (range.First is null || range.Last is null)
        {
            return null;
        }

        return (SqliteValues.ToInstant(range.First), SqliteValues.ToInstant(range.Last));
    }

    private sealed class OrderAttemptRow
    {
        public string OrderAttemptId { get; set; } = string.Empty;
        public string BrokerageId { get; set; } = string.Empty;
        public string StrategyId { get; set; } = string.Empty;
        public string Side { get; set; } = string.Empty;
        public string? RequestedQuantity { get; set; }
        public string? RequestedNotional { get; set; }
        public string ReferencePrice { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string AttemptedAtUtc { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? BrokerOrderId { get; set; }
        public string? FilledQuantity { get; set; }
        public string? AveragePrice { get; set; }
        public string? Fee { get; set; }
        public long FeeIsEstimated { get; set; }
        public string? CompletedAtUtc { get; set; }
        public string? Message { get; set; }

        internal static OrderAttemptRow From(OrderAttempt attempt)
        {
            return new OrderAttemptRow
            {
                OrderAttemptId = SqliteValues.ToText(attempt.Id),
                BrokerageId = attempt.BrokerageId,
                StrategyId = attempt.StrategyId,
                Side = attempt.Side.ToString(),
                RequestedQuantity = SqliteValues.ToText(attempt.RequestedQuantity),
                RequestedNotional = SqliteValues.ToText(attempt.RequestedNotional),
                ReferencePrice = SqliteValues.ToText(attempt.ReferencePrice),
                Reason = attempt.Reason,
                AttemptedAtUtc = SqliteValues.ToText(attempt.AttemptedAtUtc),
                Status = attempt.Status.ToString(),
                BrokerOrderId = attempt.BrokerOrderId,
                FilledQuantity = SqliteValues.ToText(attempt.FilledQuantity),
                AveragePrice = SqliteValues.ToText(attempt.AveragePrice),
                Fee = SqliteValues.ToText(attempt.Fee),
                FeeIsEstimated = attempt.FeeIsEstimated ? 1 : 0,
                CompletedAtUtc = SqliteValues.ToText(attempt.CompletedAtUtc),
                Message = attempt.Message
            };
        }

        internal OrderAttempt ToAttempt()
        {
            return new OrderAttempt(
                Guid.Parse(OrderAttemptId),
                BrokerageId,
                StrategyId,
                Enum.Parse<OrderSide>(Side),
                SqliteValues.ToNullableDecimal(RequestedQuantity),
                SqliteValues.ToNullableDecimal(RequestedNotional),
                SqliteValues.ToDecimal(ReferencePrice),
                Reason,
                SqliteValues.ToInstant(AttemptedAtUtc),
                Enum.Parse<OrderAttemptStatus>(Status),
                BrokerOrderId,
                SqliteValues.ToNullableDecimal(FilledQuantity),
                SqliteValues.ToNullableDecimal(AveragePrice),
                SqliteValues.ToNullableDecimal(Fee),
                FeeIsEstimated == 1,
                SqliteValues.ToNullableInstant(CompletedAtUtc),
                Message);
        }
    }

    private sealed class TaxLotRow
    {
        public string TaxLotId { get; set; } = string.Empty;
        public string BrokerageId { get; set; } = string.Empty;
        public string AcquiredAtUtc { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string CostBasis { get; set; } = string.Empty;
        public string RemainingQuantity { get; set; } = string.Empty;
        public string RemainingCostBasis { get; set; } = string.Empty;
        public string SourceOrderAttemptId { get; set; } = string.Empty;
        public long IsOpen { get; set; }

        internal static TaxLotRow From(TaxLot lot)
        {
            return new TaxLotRow
            {
                TaxLotId = SqliteValues.ToText(lot.Id),
                BrokerageId = lot.BrokerageId,
                AcquiredAtUtc = SqliteValues.ToText(lot.AcquiredAtUtc),
                Quantity = SqliteValues.ToText(lot.Quantity),
                CostBasis = SqliteValues.ToText(lot.CostBasis),
                RemainingQuantity = SqliteValues.ToText(lot.RemainingQuantity),
                RemainingCostBasis = SqliteValues.ToText(lot.RemainingCostBasis),
                SourceOrderAttemptId = SqliteValues.ToText(lot.SourceAttemptId),
                IsOpen = lot.RemainingQuantity > 0m ? 1 : 0
            };
        }

        internal TaxLot ToLot()
        {
            return new TaxLot(
                Guid.Parse(TaxLotId),
                BrokerageId,
                SqliteValues.ToInstant(AcquiredAtUtc),
                SqliteValues.ToDecimal(Quantity),
                SqliteValues.ToDecimal(CostBasis),
                SqliteValues.ToDecimal(RemainingQuantity),
                SqliteValues.ToDecimal(RemainingCostBasis),
                Guid.Parse(SourceOrderAttemptId));
        }
    }

    private sealed class LotDisposalRow
    {
        public string LotDisposalId { get; set; } = string.Empty;
        public string TaxLotId { get; set; } = string.Empty;
        public string BrokerageId { get; set; } = string.Empty;
        public string AcquiredAtUtc { get; set; } = string.Empty;
        public string DisposedAtUtc { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string Proceeds { get; set; } = string.Empty;
        public string CostBasis { get; set; } = string.Empty;
        public string SellOrderAttemptId { get; set; } = string.Empty;

        internal LotDisposal ToDisposal()
        {
            return new LotDisposal(
                Guid.Parse(LotDisposalId),
                Guid.Parse(TaxLotId),
                BrokerageId,
                SqliteValues.ToInstant(AcquiredAtUtc),
                SqliteValues.ToInstant(DisposedAtUtc),
                SqliteValues.ToDecimal(Quantity),
                SqliteValues.ToDecimal(Proceeds),
                SqliteValues.ToDecimal(CostBasis),
                Guid.Parse(SellOrderAttemptId));
        }
    }
}
