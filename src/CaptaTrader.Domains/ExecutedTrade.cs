using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CaptaTrader.Domains;

public class ExecutedTrade
{
    public enum TransactionTypes
    {
        Buy,
        Sell
    }

    [Key]
    public int ExecutedTradeId { get; private set; }

    [Required, ForeignKey(nameof(EndUser))]
    public int EndUserId { get; set; }

    public EndUser? EndUser { get; set; }

    [Required]
    public required DateTime TimeStamp { get; set; }

    [Required]
    public required TransactionTypes TransactionType { get; set; }

    [Required]
    public required string Symbol { get; set; }

    [Required]
    public required decimal PriceUSDT { get; set; }

    [Required]
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
