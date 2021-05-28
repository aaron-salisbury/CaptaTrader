using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CaptaTrader.Services.TradingBots
{
    public interface ITradingBotService
    {
        decimal GetAccountBalance();
        decimal GetAssetBalance(string symbol);
        decimal GetSpotPrice(string symbol);
    }
}
