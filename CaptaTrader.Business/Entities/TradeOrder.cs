using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Data.Base;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CaptaTrader.Business.Entities
{
    [Serializable]
    public class TradeOrder : BaseEntity
    {
        public enum TransactionTypes
        {
            Buy,
            Sell
        }

        [Required, Display(Name = "Time Stamp")]
        public required DateTime TimeStamp { get; set; }

        [Required, Display(Name = "Type")]
        public required TransactionTypes TransactionType { get; set; }

        [Required]
        public required string Symbol { get; set; }

        [Required, Display(Name = "Price (USDT)")]
        public required decimal PriceUSDT { get; set; }

        [Required]
        public required decimal Amount { get; set; }

        public string[] AsDataRow()
        {
            return GetNonKeyPublicProperties()
                .Select(mi => ((PropertyInfo)mi).GetValue(this)?.ToString() ?? string.Empty)
                .ToArray();
        }

        public static string[] GetColumnNames()
        {
            return GetNonKeyPublicProperties()
                 .Select(mi => mi.GetDisplayName())
                 .ToArray();
        }

        private static IEnumerable<MemberInfo> GetNonKeyPublicProperties()
        {
            return typeof(TradeOrder)
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.MemberType == MemberTypes.Property && !mi.IsKey());
        }
    }
}
