using System;
using System.Globalization;

namespace CaptaTrader.Data.Database;

/// <summary>
/// Explicit conversions between domain values and their TEXT storage, so parsing never depends on
/// the current culture or on provider-specific type mapping.
/// </summary>
internal static class SqliteValues
{
    private const string INSTANT_FORMAT = "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'";

    internal static string ToText(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    internal static string? ToText(decimal? value)
    {
        return value?.ToString(CultureInfo.InvariantCulture);
    }

    internal static decimal ToDecimal(string value)
    {
        return decimal.Parse(value, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture);
    }

    internal static decimal? ToNullableDecimal(string? value)
    {
        return value is null ? null : ToDecimal(value);
    }

    internal static string ToText(DateTimeOffset value)
    {
        return value.UtcDateTime.ToString(INSTANT_FORMAT, CultureInfo.InvariantCulture);
    }

    internal static string? ToText(DateTimeOffset? value)
    {
        return value is null ? null : ToText(value.Value);
    }

    internal static DateTimeOffset ToInstant(string value)
    {
        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    internal static DateTimeOffset? ToNullableInstant(string? value)
    {
        return value is null ? null : ToInstant(value);
    }

    internal static string ToText(Guid value)
    {
        return value.ToString("D");
    }
}
