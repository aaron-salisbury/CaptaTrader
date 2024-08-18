using System;

namespace CaptaTrader.Business.Services
{
    public interface IExchangeService
    {
        decimal? GetAccountBalance();
        decimal? GetAssetBalance(string symbol);
        decimal? GetSpotPrice(string symbol);
        bool Buy(string symbol, decimal quantity, decimal price);
        bool Sell(string symbol, decimal quantity, decimal price);
    }
}
