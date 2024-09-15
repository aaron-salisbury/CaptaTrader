using System;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices.CFGI;

public interface ICryptoFearAndGreedIndex
{
    Task<IEnumerable<int>?> GetCryptoFearAndGreedIndexAsync(int limit);
}
