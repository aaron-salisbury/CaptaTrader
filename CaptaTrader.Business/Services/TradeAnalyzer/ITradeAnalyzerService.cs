using CaptaTrader.Business.Entities;

namespace CaptaTrader.Business.Services
{
    internal interface ITradeAnalyzerService
    {
        TradeOrder? AnalyzePotentialTrade();
    }
}
