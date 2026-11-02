using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.MarketData;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CaptaTrader.Business.Modules.Providers;

/// <summary>
/// The Providers this build knows how to connect to, as registered by Integrations.
/// </summary>
public sealed class ProviderCatalog
{
    private readonly IReadOnlyList<IBrokerageProvider> _brokerages;
    private readonly IReadOnlyList<IMarketDataProvider> _marketData;

    public ProviderCatalog(IEnumerable<IBrokerageProvider> brokerages, IEnumerable<IMarketDataProvider> marketData)
    {
        ArgumentNullException.ThrowIfNull(brokerages);
        ArgumentNullException.ThrowIfNull(marketData);

        _brokerages = [.. brokerages];
        _marketData = [.. marketData];

        IEnumerable<string> ids = _brokerages.Select(provider => provider.Descriptor.Id)
            .Concat(_marketData.Select(provider => provider.Descriptor.Id));

        if (ids.GroupBy(id => id, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException("Provider identifiers must be unique.");
        }
    }

    public IReadOnlyList<ProviderDescriptor> GetDescriptors(ProviderKind kind)
    {
        return kind switch
        {
            ProviderKind.Brokerage => [.. _brokerages.Select(provider => provider.Descriptor)],
            ProviderKind.MarketData => [.. _marketData.Select(provider => provider.Descriptor)],
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    public ProviderDescriptor? FindDescriptor(string providerId)
    {
        return GetDescriptors(ProviderKind.Brokerage)
            .Concat(GetDescriptors(ProviderKind.MarketData))
            .FirstOrDefault(descriptor => string.Equals(descriptor.Id, providerId, StringComparison.OrdinalIgnoreCase));
    }

    internal IBrokerageProvider? FindBrokerage(string? providerId)
    {
        return _brokerages.FirstOrDefault(provider => string.Equals(provider.Descriptor.Id, providerId, StringComparison.OrdinalIgnoreCase));
    }

    internal IMarketDataProvider? FindMarketData(string? providerId)
    {
        return _marketData.FirstOrDefault(provider => string.Equals(provider.Descriptor.Id, providerId, StringComparison.OrdinalIgnoreCase));
    }
}
