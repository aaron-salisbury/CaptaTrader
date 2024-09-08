using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Models;

namespace CaptaTrader.Business.Services
{
    public class MockDataService : IMarketDataService
    {
        private readonly Random _random = new();

        public async Task<IEnumerable<PriceInterval>?> GetLastThirtyDaysOfPriceIntervals(string exchangeID, string baseAsset)
        {
            List<PriceInterval> priceIntervals = [];

            await Task.Run(() =>
            {
                for (int i = 0; i < 30; i++)
                {
                    DateTime startDate = DateTime.Now.Date.AddDays(-i);
                    DateTime endDate = startDate.AddDays(1).AddMinutes(-1);
                    decimal startPrice = _random.NextDecimal(55000.00M, 60000.00M).PrecisionTruncate(2);
                    decimal endPrice = _random.NextDecimal(startPrice - 250.0M, startPrice + 250.0M).PrecisionTruncate(2);

                    priceIntervals.Add(new PriceInterval()
                    {
                        Symbol = baseAsset,
                        StartDate = startDate,
                        EndDate = endDate,
                        StartPrice = startPrice,
                        EndPrice = endPrice
                    });
                }
            });

            return priceIntervals;
        }
    }
}
