using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.DTOs;
using Microsoft.Extensions.Logging;
using System.Security;

namespace CaptaTrader.Business.Services
{
    internal class KeyVaultService : IKeyVaultService
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, SecureString> _securedValuesByKeys;

        public KeyVaultService(ILogger logger, AppData appData, string passPhrase)
        {
            _logger = logger;
            _securedValuesByKeys = Task.Run(() => BuildSecuredValuesByKeysAsync(appData, passPhrase)).Result;
        }

        public SecureString? GetValueForKey(string key)
        {
            if (_securedValuesByKeys.TryGetValue(key, out SecureString? secured) && !string.IsNullOrEmpty(secured?.ToPlainString()))
            {
                return secured;
            }
            else
            {
                _logger.LogError($"Failed to locate stored value for: {key}");
                return null;
            }
        }

        private async Task<Dictionary<string, SecureString>> BuildSecuredValuesByKeysAsync(AppData appData, string passPhrase)
        {
            Dictionary<string, SecureString> securedValuesByKeys = [];

            if (!string.IsNullOrEmpty(appData.NomicsAPIKeyEncrypted))
            {
                securedValuesByKeys[nameof(AppData.NomicsAPIKeyEncrypted)] = await appData.NomicsAPIKeyEncrypted.DecryptSimpleAsync(passPhrase);
            }

            if (!string.IsNullOrEmpty(appData.BinanceAPIKeyEncrypted))
            {
                securedValuesByKeys[nameof(AppData.BinanceAPIKeyEncrypted)] = await appData.BinanceAPIKeyEncrypted.DecryptSimpleAsync(passPhrase);
            }

            if (!string.IsNullOrEmpty(appData.BinanceSecretAPIKeyEncrypted))
            {
                securedValuesByKeys[nameof(AppData.BinanceSecretAPIKeyEncrypted)] = await appData.BinanceSecretAPIKeyEncrypted.DecryptSimpleAsync(passPhrase);
            }

            return securedValuesByKeys;
        }
    }
}
