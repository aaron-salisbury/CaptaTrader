using CaptaTrader.Business.Modules.Trading.DTOs;

namespace CaptaTrader.Business.Modules.Trading.DomainServices;

public interface ITradeAnalyzerService
{
    TradeOrderDto? AnalyzePotentialTrade();
}
