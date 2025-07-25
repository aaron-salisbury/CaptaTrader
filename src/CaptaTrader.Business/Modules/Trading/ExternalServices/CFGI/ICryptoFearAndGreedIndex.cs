using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices;

internal interface ICryptoFearAndGreedIndex
{
    Task<IEnumerable<int>?> GetCryptoFearAndGreedIndexAsync(int limit);
}
