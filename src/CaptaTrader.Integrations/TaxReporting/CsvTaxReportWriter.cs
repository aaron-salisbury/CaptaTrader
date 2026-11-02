using CaptaTrader.Business.Modules.TaxReporting;
using System.Globalization;
using System.Text;

namespace CaptaTrader.Integrations.TaxReporting;

/// <summary>
/// Writes two CSV files per tax year: disposals in Form 8949 column order, and every filled order for
/// reconciliation. CSV is what tax software and preparers import, and it opens directly in Excel.
/// </summary>
/// <remarks>
/// Files are rewritten whole, via a temporary file and a replace, so a crash never leaves a half-written
/// report. Money is rounded to cents per row; totals computed from unrounded values can differ by a cent.
/// </remarks>
internal sealed class CsvTaxReportWriter : ITaxReportWriter
{
    private const string DATE_FORMAT = "yyyy-MM-dd";
    private const string INSTANT_FORMAT = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private readonly string _directory;

    public CsvTaxReportWriter(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        _directory = directory;
    }

    public async Task<TaxReportFiles> WriteAsync(TaxYearReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        Directory.CreateDirectory(_directory);

        string disposalsPath = Path.Combine(_directory, $"captatrader-{report.Year}-form8949.csv");
        string transactionsPath = Path.Combine(_directory, $"captatrader-{report.Year}-transactions.csv");

        await WriteAtomicallyAsync(disposalsPath, BuildDisposals(report), cancellationToken);
        await WriteAtomicallyAsync(transactionsPath, BuildTransactions(report), cancellationToken);

        return new TaxReportFiles(disposalsPath, transactionsPath);
    }

    internal static string BuildDisposals(TaxYearReport report)
    {
        StringBuilder builder = new();
        AppendRow(builder, "Description", "DateAcquired", "DateSold", "Proceeds", "CostBasis", "AdjustmentCode", "AdjustmentAmount", "GainOrLoss", "Term", "Brokerage", "LotId");

        foreach (TaxReportRow row in report.Disposals)
        {
            AppendRow(
                builder,
                row.Description,
                row.DateAcquired.ToString(DATE_FORMAT, CultureInfo.InvariantCulture),
                row.DateSold.ToString(DATE_FORMAT, CultureInfo.InvariantCulture),
                Money(row.Proceeds),
                Money(row.CostBasis),
                string.Empty,
                string.Empty,
                Money(row.GainOrLoss),
                row.IsLongTerm ? "Long-term" : "Short-term",
                row.Brokerage,
                row.LotId.ToString("D"));
        }

        return builder.ToString();
    }

    internal static string BuildTransactions(TaxYearReport report)
    {
        StringBuilder builder = new();
        AppendRow(builder, "FilledAtUtc", "Side", "QuantityBtc", "PriceUsd", "GrossValueUsd", "FeeUsd", "FeeIsEstimated", "Brokerage", "BrokerOrderId");

        foreach (TaxTransactionRow row in report.Transactions)
        {
            AppendRow(
                builder,
                row.FilledAtUtc.UtcDateTime.ToString(INSTANT_FORMAT, CultureInfo.InvariantCulture),
                row.Side.ToString(),
                row.Quantity.ToString("0.########", CultureInfo.InvariantCulture),
                Money(row.Price),
                Money(row.GrossValue),
                Money(row.Fee),
                row.FeeIsEstimated ? "Yes" : "No",
                row.Brokerage,
                row.BrokerOrderId ?? string.Empty);
        }

        return builder.ToString();
    }

    private static string Money(decimal value)
    {
        return Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);
    }

    /// <summary>RFC 4180: quote fields containing commas, quotes, or line breaks, doubling embedded quotes.</summary>
    private static void AppendRow(StringBuilder builder, params string[] fields)
    {
        for (int index = 0; index < fields.Length; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            string field = fields[index];

            if (field.AsSpan().IndexOfAny(",\"\r\n") >= 0)
            {
                builder.Append('"').Append(field.Replace("\"", "\"\"", StringComparison.Ordinal)).Append('"');
            }
            else
            {
                builder.Append(field);
            }
        }

        builder.Append("\r\n");
    }

    private static async Task WriteAtomicallyAsync(string path, string content, CancellationToken cancellationToken)
    {
        string temporaryPath = path + ".tmp";

        // A UTF-8 byte order mark makes Excel detect the encoding instead of guessing a code page.
        await File.WriteAllTextAsync(temporaryPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), cancellationToken);
        File.Move(temporaryPath, path, overwrite: true);
    }
}
