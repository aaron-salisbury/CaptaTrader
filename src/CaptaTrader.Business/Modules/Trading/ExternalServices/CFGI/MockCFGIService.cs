using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices;

internal sealed class MockCFGIService : ICryptoFearAndGreedIndex
{
    public async Task<IEnumerable<int>?> GetCryptoFearAndGreedIndexAsync(int limit)
    {
        List<int> indices = [];

        await Task.Run(() =>
        {
            for (int i = 0; i < limit; i++)
            {
                indices.Add(Random.Shared.Next(1, 101));
            }
        });

        return indices;
    }
}
