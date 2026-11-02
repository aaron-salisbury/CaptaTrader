using CaptaTrader.Business.Modules.Credentials;
using System;
using System.Collections.Generic;

namespace CaptaTrader.Business.Modules.Providers;

/// <summary>
/// The capability categories a user can choose a Provider for.
/// </summary>
public enum ProviderKind
{
    Brokerage,
    MarketData
}

/// <summary>
/// One secret or identifier a Provider needs, such as an API key ID or secret.
/// </summary>
/// <param name="Key">Stable key used to store the credential reference.</param>
/// <param name="Label">Prompt shown to the user.</param>
/// <param name="IsSecret">Mask input and never echo the value.</param>
public sealed record CredentialField(string Key, string Label, bool IsSecret);

/// <summary>
/// App-facing description of a Provider. Hosts present Providers through this data only, so
/// they never need to know an Integration's concrete type.
/// </summary>
/// <param name="Id">Stable identifier persisted in settings.</param>
/// <param name="IsSimulated">True when no real money or real account is involved.</param>
public sealed record ProviderDescriptor(
    string Id,
    ProviderKind Kind,
    string DisplayName,
    string Description,
    bool IsSimulated,
    IReadOnlyList<CredentialField> CredentialFields);

/// <summary>
/// Resolved credential material handed to a Provider when connecting.
/// </summary>
public sealed class ProviderConnection
{
    private readonly IReadOnlyDictionary<string, CredentialMaterial> _credentials;

    public ProviderConnection(IReadOnlyDictionary<string, CredentialMaterial> credentials)
    {
        _credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
    }

    public static ProviderConnection Anonymous { get; } = new(new Dictionary<string, CredentialMaterial>());

    public string GetRequired(string key)
    {
        if (_credentials.TryGetValue(key, out CredentialMaterial? material))
        {
            return material.Value;
        }

        throw new InvalidOperationException($"Credential '{key}' was not supplied.");
    }
}
