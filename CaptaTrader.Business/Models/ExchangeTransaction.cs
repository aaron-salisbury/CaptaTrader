using System;

namespace CaptaTrader.Business.Models
{
    public class ExchangeTransaction
    {
        public enum Types
        {
            Buy,
            Sell
        }

        public required Types Type { get; set; }
        public required DateTime Date { get; set; }
        public required string Symbol { get; set; }
        public required decimal Quantity { get; set; }
        public required decimal Price { get; set; }
        public required decimal Fee { get; set; }
        public required decimal Total { get; set; }
        public decimal? Cost { get; set; }

        public override string ToString()
        {
            return $"{Type.ToString().ToUpper()} | {Date} | Quantity: {Quantity} | Price: ${Price} | Fee: ${Fee} | Total: ${Total}";
        }
    }
}
