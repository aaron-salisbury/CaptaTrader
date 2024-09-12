using CaptaTrader.Business.DTOs;

namespace CaptaTrader.Business.Services;

public interface ITradeAnalyzerService
{
    TradeOrder? AnalyzePotentialTrade();
}
