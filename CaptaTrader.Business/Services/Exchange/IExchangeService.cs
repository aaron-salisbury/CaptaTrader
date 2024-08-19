using System;

namespace CaptaTrader.Business.Services
{
    public interface IExchangeService
    {
        decimal? GetAccountBalance();
        decimal? GetAssetBalance(string symbol);
        Task<decimal?> GetSpotPriceAsync(string symbol, string quoteAsset);
        bool Buy(string symbol, decimal quantity, decimal price);
        bool Sell(string symbol, decimal quantity, decimal price);
    }
}
