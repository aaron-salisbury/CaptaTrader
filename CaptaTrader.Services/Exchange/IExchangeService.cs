using System;

namespace CaptaTrader.Services
{
    public interface IExchangeService
    {
        decimal GetAccountBalance();
        decimal GetAssetBalance(string symbol);
        decimal GetSpotPrice(string symbol);
    }
}
