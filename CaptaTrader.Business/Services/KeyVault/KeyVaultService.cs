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

        public KeyVaultService(ILogger logger, AppData appData, SecureString encryptPhrase)
        {
            _logger = logger;
            _securedValuesByKeys = Task.Run(() => BuildSecuredValuesByKeysAsync(appData, encryptPhrase)).Result; //TODO: This fails hard sometimes, I think when pass phrase is wrong.
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

        private async Task<Dictionary<string, SecureString>> BuildSecuredValuesByKeysAsync(AppData appData, SecureString encryptPhrase)
        {
            Dictionary<string, SecureString> securedValuesByKeys = [];

            if (!string.IsNullOrEmpty(appData.CoinApiAPIKeyEncrypted))
            {
                securedValuesByKeys[nameof(AppData.CoinApiAPIKeyEncrypted)] = await appData.CoinApiAPIKeyEncrypted.DecryptSimpleAsync(encryptPhrase);
            }

            if (!string.IsNullOrEmpty(appData.BinanceAPIKeyEncrypted))
            {
                securedValuesByKeys[nameof(AppData.BinanceAPIKeyEncrypted)] = await appData.BinanceAPIKeyEncrypted.DecryptSimpleAsync(encryptPhrase);
            }

            if (!string.IsNullOrEmpty(appData.BinanceSecretAPIKeyEncrypted))
            {
                securedValuesByKeys[nameof(AppData.BinanceSecretAPIKeyEncrypted)] = await appData.BinanceSecretAPIKeyEncrypted.DecryptSimpleAsync(encryptPhrase);
            }

            return securedValuesByKeys;
        }
    }
}
