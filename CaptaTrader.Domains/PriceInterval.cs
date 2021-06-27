using System;

namespace CaptaTrader.Domains
{
    [Serializable]
    public class PriceInterval
    {
        public string Symbol { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal StartPrice { get; set; }
        public decimal EndPrice { get; set; }
        public decimal Change => StartPrice == 0 ? 0 : ((EndPrice - StartPrice) / StartPrice) * 100;
        public string Direction => Change > 0 ? "Up" : "Down";
    }
}
