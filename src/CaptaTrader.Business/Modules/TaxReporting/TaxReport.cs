using CaptaTrader.Business.Modules.Brokerage;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.TaxReporting;

/// <summary>
/// One disposal line shaped like IRS Form 8949 columns (a) through (h). Dates are in the user's
/// local time zone, which is how a tax year is determined for an individual.
/// </summary>
public sealed record TaxReportRow(
    string Description,
    DateOnly DateAcquired,
    DateOnly DateSold,
    decimal Proceeds,
    decimal CostBasis,
    decimal GainOrLoss,
    bool IsLongTerm,
    string Brokerage,
    Guid LotId);

/// <summary>
/// One filled order, for reconciliation against brokerage statements and Form 1099-DA.
/// </summary>
public sealed record TaxTransactionRow(
    DateTimeOffset FilledAtUtc,
    OrderSide Side,
    decimal Quantity,
    decimal Price,
    decimal GrossValue,
    decimal Fee,
    bool FeeIsEstimated,
    string Brokerage,
    string? BrokerOrderId);

public sealed record TaxTermTotals(decimal Proceeds, decimal CostBasis, decimal GainOrLoss);

public sealed record TaxYearReport(
    int Year,
    IReadOnlyList<TaxReportRow> Disposals,
    IReadOnlyList<TaxTransactionRow> Transactions,
    TaxTermTotals ShortTerm,
    TaxTermTotals LongTerm,
    bool IncludesSimulatedAccounts)
{
    /// <summary>Short- and long-term results net against each other on Schedule D.</summary>
    public decimal NetGainOrLoss => ShortTerm.GainOrLoss + LongTerm.GainOrLoss;
}

public sealed record TaxReportFiles(string DisposalsPath, string TransactionsPath);

/// <summary>
/// Writes a tax-year report to durable files the user can give to tax software or a preparer.
/// </summary>
public interface ITaxReportWriter
{
    Task<TaxReportFiles> WriteAsync(TaxYearReport report, CancellationToken cancellationToken = default);
}
