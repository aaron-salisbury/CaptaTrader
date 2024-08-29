using System;

namespace CaptaTrader.Business.DTOs
{
    [Serializable]
    public class Trade
    {
        public enum Types
        {
            Buy,
            Sell
        }

        public required Types Type { get; set; }
        public required decimal PriceUSDT { get; set; }
        public required decimal Amount { get; set; }
    }
}
