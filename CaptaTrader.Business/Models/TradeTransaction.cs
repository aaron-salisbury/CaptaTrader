using System.ComponentModel;

namespace CaptaTrader.Business.Models
{
    public class TradeTransaction
    {
        public enum TransactionTypes
        {
            Buy,
            Sell
        }

        [DisplayName("Time Stamp")]
        public DateTime TimeStamp { get; set; }

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
    }
}
