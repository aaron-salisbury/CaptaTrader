using System;

namespace CaptaTrader.Business.DTOs;

[Serializable]
public record TradeOrder
{
    public enum TransactionTypes
    {
        Buy,
        Sell
    }

    public required DateTime TimeStamp { get; set; }

    public required TransactionTypes TransactionType { get; set; }

    public required string Symbol { get; set; }

    public required decimal PriceUSDT { get; set; }

    public required decimal Amount { get; set; }

    //public string[] AsDataRow()
    //{
    //    return GetNonKeyPublicProperties()
    //        .Select(mi => ((PropertyInfo)mi).GetValue(this)?.ToString() ?? string.Empty)
    //        .ToArray();
    //}

    //public static string[] GetColumnNames()
    //{
    //    return GetNonKeyPublicProperties()
    //         .Select(mi => mi.GetDisplayName())
    //         .ToArray();
    //}

    //private static IEnumerable<MemberInfo> GetNonKeyPublicProperties()
    //{
    //    return typeof(TradeOrder)
    //        .GetMembers(BindingFlags.Public | BindingFlags.Instance)
    //        .Where(mi => mi.MemberType == MemberTypes.Property && !mi.IsKey());
    //}
}
