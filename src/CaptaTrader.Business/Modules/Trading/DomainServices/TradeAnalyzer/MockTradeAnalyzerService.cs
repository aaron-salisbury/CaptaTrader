using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Modules.Trading.DTOs;
using System;

namespace CaptaTrader.Business.Modules.Trading.DomainServices;

public sealed class MockTradeAnalyzerService : ITradeAnalyzerService
{
    public TradeOrderDto? AnalyzePotentialTrade()
    {
        if (Random.Shared.Next(3) == 0)
        {
            // Simulate a third of the time no good trade being found.
            return null;
        }

        TradeOrderDto tradeOrder = CreateMockTradeTransaction();

        return tradeOrder;
    }

    private static TradeOrderDto CreateMockTradeTransaction()
    {
        TradeOrderDto.TransactionTypes transactionType;
        string symbol;

        if (Random.Shared.Next(2) == 1)
        {
            transactionType = TradeOrderDto.TransactionTypes.Buy;
            symbol = "BTC";
        }
        else
        {
            transactionType = TradeOrderDto.TransactionTypes.Sell;
            symbol = "USDT";
        }

        decimal quantity = Random.Shared.NextDecimal(0.001M, 0.01M).PrecisionTruncate(3);
        decimal price = Random.Shared.NextDecimal(55000.00M, 60000.00M).PrecisionTruncate(2);

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
