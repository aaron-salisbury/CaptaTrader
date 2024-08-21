// Ignore Spelling: Binance Api App

using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Data.Attributes;
using System.Security;
using System.Text.Json.Serialization;

namespace CaptaTrader.Business.DTOs
{
    [Serializable, BaseName("CaptaTrader")]
    public class AppData
    {
        public string? Email { get; set; }
        [JsonInclude] internal string? CoinApiAPIKeyEncrypted { get; private set; }
        [JsonInclude] internal string? BinanceAPIKeyEncrypted { get; private set; }
        [JsonInclude] internal string? BinanceSecretAPIKeyEncrypted { get; private set; }

        public async Task SetCoinApiAPIKeyAsync(SecureString securedInput, SecureString encryptPhrase)
        {
            CoinApiAPIKeyEncrypted = await EncryptSecuredInputAsync(securedInput, encryptPhrase);
        }

        public async Task SetBinanceAPIKeyAsync(SecureString securedInput, SecureString encryptPhrase)
        {
            BinanceAPIKeyEncrypted = await EncryptSecuredInputAsync(securedInput, encryptPhrase);
        }

        public async Task SetBinanceSecretAPIKeyAsync(SecureString securedInput, SecureString encryptPhrase)
        {
            BinanceSecretAPIKeyEncrypted = await EncryptSecuredInputAsync(securedInput, encryptPhrase);
        }

        private static async Task<string> EncryptSecuredInputAsync(SecureString securedInput, SecureString encryptPhrase)
        {
            string input = securedInput.ToPlainString();

            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            else
            {
                return await input.EncryptSimpleAsync(encryptPhrase);
            }
        }
    }
}
