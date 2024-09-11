using System;

namespace CaptaTrader.Business.Services;

public class MockCFGIService : ICryptoFearAndGreedIndex
{
    private readonly Random _random = new();

    public async Task<IEnumerable<int>?> GetCryptoFearAndGreedIndexAsync(int limit)
    {
        List<int> indicies = [];

        await Task.Run(() =>
        {
            for (int i = 0; i < limit; i++)
            {
                indicies.Add(_random.Next(1, 101));
            }
        });

        return indicies;
    }
}
