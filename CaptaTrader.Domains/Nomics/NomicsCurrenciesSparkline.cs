using System;
using System.Collections.Generic;

namespace CaptaTrader.Domains
{
    [Serializable]
    public class NomicsCurrenciesSparkline
    {
        public string Currency { get; set; }
        public List<DateTime> Timestamps { get; set; }
        public List<string> Prices { get; set; }
    }
}
