using CaptaTrader.Business.Entities;

namespace CaptaTrader.Business.Services
{
    public interface ITradeAnalyzerService
    {
        TradeOrder? AnalyzePotentialTrade();
    }
}
