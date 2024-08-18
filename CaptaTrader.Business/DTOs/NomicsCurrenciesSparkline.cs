using System;

namespace CaptaTrader.Business.DTOs
{
    [Serializable]
    public class NomicsCurrenciesSparkline
    {
        public required string Currency { get; set; }
        public required List<DateTime> Timestamps { get; set; }
        public required List<string> Prices { get; set; }
    }
}
