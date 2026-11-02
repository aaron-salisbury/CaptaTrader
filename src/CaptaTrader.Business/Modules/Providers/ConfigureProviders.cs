using CaptaTrader.Business.Modules.Brokerage;
using CaptaTrader.Business.Modules.Capabilities;
using CaptaTrader.Business.Modules.Credentials;
using CaptaTrader.Business.Modules.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Providers;

public sealed record CredentialFieldStatus(CredentialField Field, bool IsSet);

/// <summary>
/// A Provider as configured by the user: whether it is the selection for its kind and which
/// credentials have been supplied. Secret values are never part of this shape.
/// </summary>
public sealed record ProviderStatus(ProviderDescriptor Descriptor, bool IsSelected, IReadOnlyList<CredentialFieldStatus> Fields)
{
    public bool IsReady => Fields.All(status => status.IsSet);
}

/// <summary>
/// Selects Providers per capability category and manages their credentials. Credential values go
/// to the OS credential store; settings keep only an opaque reference.
/// </summary>
public sealed class ConfigureProviders
{
    private const string CREDENTIAL_STORE_NAME = "The OS credential store";

    private readonly ProviderCatalog _catalog;
    private readonly IApplicationSettingStore _settingStore;
    private readonly ICredentialStore _credentialStore;
    private readonly TimeProvider _timeProvider;

    public ConfigureProviders(ProviderCatalog catalog, IApplicationSettingStore settingStore, ICredentialStore credentialStore, TimeProvider timeProvider)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _settingStore = settingStore ?? throw new ArgumentNullException(nameof(settingStore));
        _credentialStore = credentialStore ?? throw new ArgumentNullException(nameof(credentialStore));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Returns the selected Provider for <paramref name="kind"/>, defaulting to the first registered
    /// one. Integrations registers a simulated brokerage first, so a fresh install never trades real money.
    /// </summary>
    public async Task<ProviderDescriptor> GetSelectedAsync(ProviderKind kind, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProviderDescriptor> descriptors = _catalog.GetDescriptors(kind);
        string? selectedId = await _settingStore.GetAsync(SelectionKey(kind), cancellationToken);

        return descriptors.FirstOrDefault(descriptor => string.Equals(descriptor.Id, selectedId, StringComparison.OrdinalIgnoreCase))
            ?? descriptors[0];
    }

    public async Task<IReadOnlyList<ProviderStatus>> GetStatusesAsync(ProviderKind kind, CancellationToken cancellationToken = default)
    {
        ProviderDescriptor selected = await GetSelectedAsync(kind, cancellationToken);
        List<ProviderStatus> statuses = [];

        foreach (ProviderDescriptor descriptor in _catalog.GetDescriptors(kind))
        {
            List<CredentialFieldStatus> fields = [];

            foreach (CredentialField field in descriptor.CredentialFields)
            {
                string? reference = await _settingStore.GetAsync(CredentialKey(descriptor, field), cancellationToken);
                fields.Add(new CredentialFieldStatus(field, !string.IsNullOrWhiteSpace(reference)));
            }

            statuses.Add(new ProviderStatus(descriptor, descriptor.Id == selected.Id, fields));
        }

        return statuses;
    }

    public async Task<CapabilityResult<ProviderDescriptor>> SelectAsync(ProviderKind kind, string providerId, CancellationToken cancellationToken = default)
    {
        ProviderDescriptor? descriptor = _catalog.GetDescriptors(kind)
            .FirstOrDefault(candidate => string.Equals(candidate.Id, providerId, StringComparison.OrdinalIgnoreCase));

        if (descriptor is null)
        {
            return CapabilityResult<ProviderDescriptor>.Failure(CapabilityStatus.NotFound, "That provider is not available.", _timeProvider.GetUtcNow());
        }

        await _settingStore.SetAsync(SelectionKey(kind), descriptor.Id, cancellationToken);

        return CapabilityResult<ProviderDescriptor>.Success(descriptor, _timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Selects, for every kind, the first simulated Provider that needs no credentials. Demo mode uses
    /// this so it can run without the host knowing any particular Provider.
    /// </summary>
    public async Task<CapabilityResult<bool>> SelectSimulatedProvidersAsync(CancellationToken cancellationToken = default)
    {
        foreach (ProviderKind kind in Enum.GetValues<ProviderKind>())
        {
            ProviderDescriptor? simulated = _catalog.GetDescriptors(kind)
                .FirstOrDefault(descriptor => descriptor.IsSimulated && descriptor.CredentialFields.Count == 0);

            if (simulated is null)
            {
                return CapabilityResult<bool>.Failure(CapabilityStatus.NotFound, $"No simulated {kind} provider is installed.", _timeProvider.GetUtcNow());
            }

            await _settingStore.SetAsync(SelectionKey(kind), simulated.Id, cancellationToken);
        }

        return CapabilityResult<bool>.Success(true, _timeProvider.GetUtcNow());
    }

    public async Task<CapabilityResult<bool>> SetCredentialAsync(string providerId, string fieldKey, string value, CancellationToken cancellationToken = default)
    {
        (ProviderDescriptor descriptor, CredentialField field)? target = FindField(providerId, fieldKey);

        if (target is null)
        {
            return CapabilityResult<bool>.Failure(CapabilityStatus.NotFound, "That credential is not used by this provider.", _timeProvider.GetUtcNow());
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return CapabilityResult<bool>.Failure(CapabilityStatus.InvalidRequest, "A value is required.", _timeProvider.GetUtcNow());
        }

        string settingKey = CredentialKey(target.Value.descriptor, target.Value.field);
        string? existing = await _settingStore.GetAsync(settingKey, cancellationToken);
        CredentialReference reference = string.IsNullOrWhiteSpace(existing) ? CredentialReference.New() : new CredentialReference(existing);

        try
        {
            await _credentialStore.StoreAsync(reference, new CredentialMaterial(value.Trim()), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return CapabilityFailure.ToResult<bool>(exception, CREDENTIAL_STORE_NAME, _timeProvider.GetUtcNow());
        }

        await _settingStore.SetAsync(settingKey, reference.Value, cancellationToken);

        return CapabilityResult<bool>.Success(true, _timeProvider.GetUtcNow());
    }

    public async Task<CapabilityResult<bool>> ClearCredentialsAsync(string providerId, CancellationToken cancellationToken = default)
    {
        ProviderDescriptor? descriptor = _catalog.FindDescriptor(providerId);

        if (descriptor is null)
        {
            return CapabilityResult<bool>.Failure(CapabilityStatus.NotFound, "That provider is not available.", _timeProvider.GetUtcNow());
        }

        try
        {
            foreach (CredentialField field in descriptor.CredentialFields)
            {
                string settingKey = CredentialKey(descriptor, field);
                string? existing = await _settingStore.GetAsync(settingKey, cancellationToken);

                if (!string.IsNullOrWhiteSpace(existing))
                {
                    await _credentialStore.DeleteAsync(new CredentialReference(existing), cancellationToken);
                    await _settingStore.DeleteAsync(settingKey, cancellationToken);
                }
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return CapabilityFailure.ToResult<bool>(exception, CREDENTIAL_STORE_NAME, _timeProvider.GetUtcNow());
        }

        return CapabilityResult<bool>.Success(true, _timeProvider.GetUtcNow());
    }

    /// <summary>
    /// Connects to the selected brokerage and reads balances, proving the credentials work.
    /// </summary>
    public async Task<CapabilityResult<AccountSnapshot>> TestBrokerageAsync(CancellationToken cancellationToken = default)
    {
        ProviderDescriptor descriptor = await GetSelectedAsync(ProviderKind.Brokerage, cancellationToken);
        IBrokerageProvider provider = _catalog.FindBrokerage(descriptor.Id)!;
        CapabilityResult<ProviderConnection> connection = await ResolveConnectionAsync(descriptor, cancellationToken);

        if (!connection.IsSuccess)
        {
            return connection.PropagateFailure<AccountSnapshot>();
        }

        try
        {
            AccountSnapshot account = await provider.Connect(connection.Value!).GetAccountAsync(cancellationToken);
            return CapabilityResult<AccountSnapshot>.Success(account, _timeProvider.GetUtcNow());
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            return CapabilityFailure.ToResult<AccountSnapshot>(exception, descriptor.DisplayName, _timeProvider.GetUtcNow());
        }
    }

    internal async Task<CapabilityResult<ProviderConnection>> ResolveConnectionAsync(ProviderDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        Dictionary<string, CredentialMaterial> credentials = [];

        try
        {
            foreach (CredentialField field in descriptor.CredentialFields)
            {
                string? reference = await _settingStore.GetAsync(CredentialKey(descriptor, field), cancellationToken);
                CredentialMaterial? material = string.IsNullOrWhiteSpace(reference)
                    ? null
                    : await _credentialStore.GetAsync(new CredentialReference(reference), cancellationToken);

                if (material is null)
                {
                    return CapabilityResult<ProviderConnection>.Failure(
                        CapabilityStatus.NotConfigured,
                        $"{descriptor.DisplayName} needs '{field.Label}'. Set it under Settings > API keys.",
                        _timeProvider.GetUtcNow());
                }

                credentials[field.Key] = material;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return CapabilityFailure.ToResult<ProviderConnection>(exception, CREDENTIAL_STORE_NAME, _timeProvider.GetUtcNow());
        }

        return CapabilityResult<ProviderConnection>.Success(new ProviderConnection(credentials), _timeProvider.GetUtcNow());
    }

    private (ProviderDescriptor descriptor, CredentialField field)? FindField(string providerId, string fieldKey)
    {
        ProviderDescriptor? descriptor = _catalog.FindDescriptor(providerId);
        CredentialField? field = descriptor?.CredentialFields.FirstOrDefault(candidate => candidate.Key == fieldKey);

        if (descriptor is null || field is null)
        {
            return null;
        }

        return (descriptor, field);
    }

    private static string SelectionKey(ProviderKind kind)
    {
        return $"provider.selected.{kind}";
    }

    private static string CredentialKey(ProviderDescriptor descriptor, CredentialField field)
    {
        return $"credential.{descriptor.Id}.{field.Key}";
    }
}
