using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Services;

namespace CaptaTrader.Business.Models
{
    public class MockTradeAnalyzerService : ITradeAnalyzerService
    {
        private readonly Random _random = new();

        public TradeTransaction? AnalyzePotentialTrade()
        {
            if (_random.Next(3) == 0)
            {
                // Simulate a third of the time no good trade being found.
                return null;
            }

            return CreateMockTradeTransaction();
        }

        private TradeTransaction CreateMockTradeTransaction()
        {
            TradeTransaction.TransactionTypes transactionType = _random.Next(2) == 1
                ? TradeTransaction.TransactionTypes.Buy
                : TradeTransaction.TransactionTypes.Sell;

            decimal quantity = _random.NextDecimal(0.001M, 0.01M).PrecisionTruncate(3);
            decimal price = _random.NextDecimal(55000.00M, 60000.00M).PrecisionTruncate(2);
            decimal availableUSD = _random.NextDecimal(100000.0M, 200000.0M).PrecisionTruncate(2);
            decimal availableBTC = _random.NextDecimal(1.0M, 2.0M).PrecisionTruncate(2);
            decimal total = quantity * price;

            return new TradeTransaction()
            {
                TimeStamp = DateTime.Now,
                TransactionType = transactionType,
                Quantity = quantity,
                Price = price,
                Total = total,
                AvailableUSD = availableUSD,
                LockedUSD = 0.0M,
                AvailableBTC = availableBTC,
                LockedBTC = 0.0M
            };
        }
    }
}
