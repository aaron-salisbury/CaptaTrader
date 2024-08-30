using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Services
{
    public interface ITradeAnalyzerService
    {
        TradeOrder? AnalyzePotentialTrade();
    }
}
