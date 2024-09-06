using System;

namespace CaptaTrader.Business.Services
{
    public interface ICryptoFearAndGreedIndex
    {
        Task<IEnumerable<int>?> GetCryptoFearAndGreedIndexAsync(int limit);
    }
}
