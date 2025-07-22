using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Trading.ExternalServices;

public sealed class MockCFGIService : ICryptoFearAndGreedIndex
{
    private readonly Random _random = new();

    public async Task<IEnumerable<int>?> GetCryptoFearAndGreedIndexAsync(int limit)
    {
        List<int> indices = [];

        await Task.Run(() =>
        {
            for (int i = 0; i < limit; i++)
            {
                indices.Add(_random.Next(1, 101));
            }
        });

        return indices;
    }
}
