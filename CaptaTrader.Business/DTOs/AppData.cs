using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Data.Attributes;
using System.Security;

namespace CaptaTrader.Business.DTOs
{
    [Serializable, BaseName("CaptaTrader")]
    public class AppData
    {
        public string? Email { get; set; }

        public string? CoinApiAPIKeyEncrypted { get; set; }
        public string? BinanceAPIKeyEncrypted { get; set; }
        public string? BinanceSecretAPIKeyEncrypted { get; set; }

        public async Task SetCoinApiAPIKeyAsync(string input, SecureString encryptPhrase)
        {
            if (string.IsNullOrEmpty(input))
            {
                CoinApiAPIKeyEncrypted = input;
            }
            else
            {
                CoinApiAPIKeyEncrypted = await input.EncryptSimpleAsync(encryptPhrase);
            }
        }

        public async Task SetBinanceAPIKeyAsync(string input, SecureString encryptPhrase)
        {
            if (string.IsNullOrEmpty(input))
            {
                BinanceAPIKeyEncrypted = input;
            }
            else
            {
                BinanceAPIKeyEncrypted = await input.EncryptSimpleAsync(encryptPhrase);
            }
        }

        public async Task SetBinanceSecretAPIKeyAsync(string input, SecureString encryptPhrase)
        {
            if (string.IsNullOrEmpty(input))
            {
                BinanceSecretAPIKeyEncrypted = input;
            }
            else
            {
                BinanceSecretAPIKeyEncrypted = await input.EncryptSimpleAsync(encryptPhrase);
            }
        }
    }
}
