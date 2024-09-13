using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.DTOs;
using CaptaTrader.Business.Services;

namespace CaptaTrader.Business.Models;

public class MockTradeAnalyzerService : ITradeAnalyzerService
{
    private readonly Random _random = new();

    public TradeOrder? AnalyzePotentialTrade()
    {
        if (_random.Next(3) == 0)
        {
            // Simulate a third of the time no good trade being found.
            return null;
        }

        TradeOrder tradeOrder = CreateMockTradeTransaction();

        return tradeOrder;
    }

    private TradeOrder CreateMockTradeTransaction()
    {
        TradeOrder.TransactionTypes transactionType;
        string symbol;

        if (_random.Next(2) == 1)
        {
            transactionType = TradeOrder.TransactionTypes.Buy;
            symbol = "BTC";
        }
        else
        {
            transactionType = TradeOrder.TransactionTypes.Sell;
            symbol = "USDT";
        }

        decimal quantity = _random.NextDecimal(0.001M, 0.01M).PrecisionTruncate(3);
        decimal price = _random.NextDecimal(55000.00M, 60000.00M).PrecisionTruncate(2);

        return new TradeOrder()
        {
            TimeStamp = DateTime.Now,
            TransactionType = transactionType,
            Symbol = symbol,
            PriceUSDT = price,
            Amount = quantity
        };
    }
}
