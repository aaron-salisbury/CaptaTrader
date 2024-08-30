using CaptaTrader.Business.Base;
using CaptaTrader.Business.Base.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CaptaTrader.Business.Models
{
    [Serializable]
    public class TradeOrder : IEntity
    {
        public enum TransactionTypes
        {
            Buy,
            Sell
        }

        [Key]
        public int Id { get; set; }

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
