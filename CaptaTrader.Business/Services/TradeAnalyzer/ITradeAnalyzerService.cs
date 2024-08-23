using CaptaTrader.Models;

namespace CaptaTrader.Business.Services
{
    public interface ITradeAnalyzerService
    {
        TradeTransaction? AnalyzePotentialTrade();
    }
}
