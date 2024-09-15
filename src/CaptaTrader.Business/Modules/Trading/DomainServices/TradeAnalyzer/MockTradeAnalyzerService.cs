using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Modules.Trading.DTOs;

namespace CaptaTrader.Business.Modules.Trading.DomainServices.TradeAnalyzer;

public class MockTradeAnalyzerService : ITradeAnalyzerService
{
    private readonly Random _random = new();

    public TradeOrderDto? AnalyzePotentialTrade()
    {
        if (_random.Next(3) == 0)
        {
            // Simulate a third of the time no good trade being found.
            return null;
        }

        TradeOrderDto tradeOrder = CreateMockTradeTransaction();

        return tradeOrder;
    }

    private TradeOrderDto CreateMockTradeTransaction()
    {
        TradeOrderDto.TransactionTypes transactionType;
        string symbol;

        if (_random.Next(2) == 1)
        {
            transactionType = TradeOrderDto.TransactionTypes.Buy;
            symbol = "BTC";
        }
        else
        {
            transactionType = TradeOrderDto.TransactionTypes.Sell;
            symbol = "USDT";
        }

        decimal quantity = _random.NextDecimal(0.001M, 0.01M).PrecisionTruncate(3);
        decimal price = _random.NextDecimal(55000.00M, 60000.00M).PrecisionTruncate(2);

        return new TradeOrderDto()
        {
            TimeStamp = DateTime.Now,
            TransactionType = transactionType,
            Symbol = symbol,
            PriceUSDT = price,
            Amount = quantity
        };
    }
}
