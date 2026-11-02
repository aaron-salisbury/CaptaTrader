using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Ledger;

public interface ILedgerStore
{
    Task InsertAttemptAsync(OrderAttempt attempt, CancellationToken cancellationToken = default);

    /// <summary>Saves the attempt outcome and its lot changes in one transaction.</summary>
    Task CompleteAttemptAsync(OrderAttempt attempt, LedgerChanges changes, CancellationToken cancellationToken = default);

    /// <summary>Marks attempts left <see cref="OrderAttemptStatus.Pending"/> by an earlier run as unknown.</summary>
    /// <returns>The number of attempts marked.</returns>
    Task<int> MarkAbandonedPendingAsUnknownAsync(string message, CancellationToken cancellationToken = default);

    /// <summary><see cref="OrderAttemptStatus.Unknown"/> attempts for one brokerage account, oldest first.</summary>
    Task<IReadOnlyList<OrderAttempt>> GetUnknownAttemptsAsync(string brokerageId, CancellationToken cancellationToken = default);

    /// <summary>When each attempt for one brokerage account at or after <paramref name="sinceUtc"/> was made, oldest first.</summary>
    Task<IReadOnlyList<DateTimeOffset>> GetAttemptTimesSinceAsync(string brokerageId, DateTimeOffset sinceUtc, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderAttempt>> GetRecentAttemptsAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderAttempt>> GetFilledAttemptsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);

    /// <summary>Open lots for one brokerage account, oldest first.</summary>
    Task<IReadOnlyList<TaxLot>> GetOpenLotsAsync(string brokerageId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LotDisposal>> GetDisposalsAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);

    /// <summary>Earliest and latest fill times on the ledger, or null when nothing has filled.</summary>
    Task<(DateTimeOffset First, DateTimeOffset Last)?> GetFillRangeAsync(CancellationToken cancellationToken = default);
}
