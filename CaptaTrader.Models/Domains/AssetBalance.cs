using System;

namespace CaptaTrader.Models
{
    public class AssetBalance
    {
        public required string Asset { get; set; }
        public decimal Available { get; set; }
        public decimal Locked { get; set; }
        public decimal Total { get; set; }
    }
}
