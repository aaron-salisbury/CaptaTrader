using CaptaTrader.Models.Base.Extensions;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace CaptaTrader.Models
{
    [Serializable]
    public class TradeTransaction
    {
        public enum TransactionTypes
        {
            Buy,
            Sell
        }

        [Key]
        public long TradeTransactionId { get; set; }

        [DisplayName("Time Stamp")]
        public required DateTime TimeStamp { get; set; }

        [DisplayName("Type")]
        public TransactionTypes TransactionType { get; set; }

        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }

        [DisplayName("Available USD")]
        public decimal AvailableUSD { get; set; }

        [DisplayName("Locked USD")]
        public decimal LockedUSD { get; set; }

        [DisplayName("Available BTC")]
        public decimal AvailableBTC { get; set; }

        [DisplayName("Locked BTC")]
        public decimal LockedBTC { get; set; }

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
            return typeof(TradeTransaction)
                .GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(mi => mi.MemberType == MemberTypes.Property && !mi.IsKey());
        }
    }
}
