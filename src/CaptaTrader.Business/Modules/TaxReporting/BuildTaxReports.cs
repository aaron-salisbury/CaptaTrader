using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.Ledger;
using CaptaTrader.Business.Modules.Providers;
using CaptaTrader.Business.Modules.UserAccess;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.TaxReporting;

/// <summary>
/// Derives per-tax-year reports from the ledger, which stays the single source of truth; files are
/// regenerated rather than appended so corrections to the ledger always flow through.
/// </summary>
/// <remarks>
/// Simulated (paper) accounts are excluded outside demo mode so practice trades never reach a tax file.
/// </remarks>
public sealed class BuildTaxReports
{
    private const string WRITER_NAME = "The tax report writer";

    private readonly ILedgerStore _ledger;
    private readonly ITaxReportWriter _writer;
    private readonly ProviderCatalog _catalog;
    private readonly UserSession _session;
    private readonly TimeProvider _timeProvider;

    public BuildTaxReports(ILedgerStore ledger, ITaxReportWriter writer, ProviderCatalog catalog, UserSession session, TimeProvider timeProvider)
    {
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>Local tax years that have fills, newest first.</summary>
    public async Task<IReadOnlyList<int>> GetYearsAsync(CancellationToken cancellationToken = default)
    {
        (DateTimeOffset First, DateTimeOffset Last)? range = await _ledger.GetFillRangeAsync(cancellationToken);

        if (range is null)
        {
            return [];
        }

        int first = TaxYearOf(range.Value.First);
        int last = TaxYearOf(range.Value.Last);

        return [.. Enumerable.Range(first, last - first + 1).Reverse()];
    }

    public int TaxYearOf(DateTimeOffset instant)
    {
        return TimeZoneInfo.ConvertTime(instant, _timeProvider.LocalTimeZone).Year;
    }

    public async Task<TaxYearReport> BuildAsync(int year, CancellationToken cancellationToken = default)
    {
        (DateTimeOffset fromUtc, DateTimeOffset toUtc) = YearBounds(year);
        bool includeSimulated = _session.IsDemo;

        IReadOnlyList<LotDisposal> disposals = await _ledger.GetDisposalsAsync(fromUtc, toUtc, cancellationToken);
        IReadOnlyList<OrderAttempt> fills = await _ledger.GetFilledAttemptsAsync(fromUtc, toUtc, cancellationToken);

        List<TaxReportRow> rows = [.. disposals
            .Where(disposal => includeSimulated || !IsSimulated(disposal.BrokerageId))
            .OrderBy(disposal => disposal.DisposedAtUtc)
            .ThenBy(disposal => disposal.AcquiredAtUtc)
            .Select(ToRow)];

        List<TaxTransactionRow> transactions = [.. fills
            .Where(fill => includeSimulated || !IsSimulated(fill.BrokerageId))
            .OrderBy(fill => fill.CompletedAtUtc)
            .Select(fill => new TaxTransactionRow(
                fill.CompletedAtUtc ?? fill.AttemptedAtUtc,
                fill.Side,
                fill.FilledQuantity ?? 0m,
                fill.AveragePrice ?? 0m,
                fill.GrossValue ?? 0m,
                fill.Fee ?? 0m,
                fill.FeeIsEstimated,
                BrokerageName(fill.BrokerageId),
                fill.BrokerOrderId))];

        return new TaxYearReport(
            year,
            rows,
            transactions,
            Totals(rows.Where(row => !row.IsLongTerm)),
            Totals(rows.Where(row => row.IsLongTerm)),
            includeSimulated);
    }

    public async Task<CapabilityResult<TaxReportFiles>> ExportAsync(int year, CancellationToken cancellationToken = default)
    {
        TaxYearReport report = await BuildAsync(year, cancellationToken);

        try
        {
            TaxReportFiles files = await _writer.WriteAsync(report, cancellationToken);
            return CapabilityResult<TaxReportFiles>.Success(files, _timeProvider.GetUtcNow());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return CapabilityFailure.ToResult<TaxReportFiles>(exception, WRITER_NAME, _timeProvider.GetUtcNow());
        }
    }

    private TaxReportRow ToRow(LotDisposal disposal)
    {
        DateOnly acquired = LocalDate(disposal.AcquiredAtUtc);
        DateOnly sold = LocalDate(disposal.DisposedAtUtc);

        return new TaxReportRow(
            string.Create(CultureInfo.InvariantCulture, $"{disposal.Quantity:0.########} BTC"),
            acquired,
            sold,
            disposal.Proceeds,
            disposal.CostBasis,
            disposal.GainOrLoss,
            LotAccounting.IsLongTerm(acquired, sold),
            BrokerageName(disposal.BrokerageId),
            disposal.LotId);
    }

    private static TaxTermTotals Totals(IEnumerable<TaxReportRow> rows)
    {
        List<TaxReportRow> list = [.. rows];

        return new TaxTermTotals(list.Sum(row => row.Proceeds), list.Sum(row => row.CostBasis), list.Sum(row => row.GainOrLoss));
    }

    private (DateTimeOffset FromUtc, DateTimeOffset ToUtc) YearBounds(int year)
    {
        TimeZoneInfo zone = _timeProvider.LocalTimeZone;
        DateTime start = new(year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        DateTime end = start.AddYears(1);

        return (
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(start, zone), TimeSpan.Zero),
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(end, zone), TimeSpan.Zero));
    }

    private DateOnly LocalDate(DateTimeOffset instant)
    {
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(instant, _timeProvider.LocalTimeZone).DateTime);
    }

    private bool IsSimulated(string brokerageId)
    {
        return _catalog.FindDescriptor(brokerageId)?.IsSimulated ?? false;
    }

    private string BrokerageName(string brokerageId)
    {
        return _catalog.FindDescriptor(brokerageId)?.DisplayName ?? brokerageId;
    }
}
