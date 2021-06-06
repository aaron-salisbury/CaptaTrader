using System.Collections.Generic;

namespace CaptaTrader.Services.TradingBots
{
    public interface ITradingBotService
    {
        IEnumerable<string> ActivateAutoTrading();
        decimal GetAccountBalance();
        decimal GetAssetBalance(string symbol);
        decimal GetSpotPrice(string symbol);
    }
}
