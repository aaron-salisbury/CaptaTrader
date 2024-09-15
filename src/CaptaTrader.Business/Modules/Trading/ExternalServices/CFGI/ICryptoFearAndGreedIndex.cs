using System;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices;

public interface ICryptoFearAndGreedIndex
{
    Task<IEnumerable<int>?> GetCryptoFearAndGreedIndexAsync(int limit);
}
