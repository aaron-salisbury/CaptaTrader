using CaptaTrader.Business.Modules.Trading.DTOs;

namespace CaptaTrader.Business.Modules.Trading.DomainServices.TradeAnalyzer;

public interface ITradeAnalyzerService
{
    TradeOrderDto? AnalyzePotentialTrade();
}
