using System;

namespace CaptaTrader.Core.Base.Extensions
{
    public static class DecimalExtensions
    {
        public static decimal PrecisionTruncate(this decimal d, byte precision)
        {
            // https://stackoverflow.com/questions/3143657/truncate-two-decimal-places-without-rounding

            decimal r = Math.Round(d, precision);

            if (d > 0 && r > d)
            {
                return r - new decimal(1, 0, 0, false, precision);
            }
            else if (d < 0 && r < d)
            {
                return r + new decimal(1, 0, 0, false, precision);
            }

            return r;
        }
    }
}
