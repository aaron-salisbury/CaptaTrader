using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.Settings;

/// <summary>
/// Persists simple application-owned key/value settings. Secrets never go here; store an opaque
/// <see cref="Credentials.CredentialReference"/> instead.
/// </summary>
public interface IApplicationSettingStore
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
