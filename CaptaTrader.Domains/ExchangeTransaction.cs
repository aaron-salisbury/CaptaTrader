using System;

namespace CaptaTrader.Domains
{
    [Serializable]
    public class ExchangeTransaction
    {
        public enum Types
        {
            Buy,
            Sell
        }

        public Types Type { get; set; }
        public DateTime Date { get; set; }
        public string Symbol { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public decimal Fee { get; set; }
        public decimal Total { get; set; }

        public override string ToString()
        {
            return $"{Type.ToString().ToUpper()} | {Date} | Quantity: {Quantity} | Price: ${Price} | Fee: ${Fee} | Total: ${Total}";
        }
    }
}
