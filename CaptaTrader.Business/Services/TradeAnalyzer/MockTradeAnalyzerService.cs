using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Entities;
using CaptaTrader.Business.Services;

namespace CaptaTrader.Business.Models
{
    internal class MockTradeAnalyzerService : ITradeAnalyzerService
    {
        private readonly Random _random = new();

        public TradeOrder? AnalyzePotentialTrade()
        {
            if (_random.Next(3) == 0)
            {
                // Simulate a third of the time no good trade being found.
                return null;
            }

            return CreateMockTradeTransaction();
        }

        private TradeOrder CreateMockTradeTransaction()
        {
            TradeOrder.TransactionTypes transactionType = _random.Next(2) == 1
                ? TradeOrder.TransactionTypes.Buy
                : TradeOrder.TransactionTypes.Sell;

            decimal quantity = _random.NextDecimal(0.001M, 0.01M).PrecisionTruncate(3);
            decimal price = _random.NextDecimal(55000.00M, 60000.00M).PrecisionTruncate(2);
            decimal availableUSD = _random.NextDecimal(100000.0M, 200000.0M).PrecisionTruncate(2);
            decimal availableBTC = _random.NextDecimal(1.0M, 2.0M).PrecisionTruncate(2);
            decimal total = quantity * price;

            return new TradeOrder()
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
