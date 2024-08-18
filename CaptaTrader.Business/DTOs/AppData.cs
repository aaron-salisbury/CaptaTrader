using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Data.Attributes;

namespace CaptaTrader.Business.DTOs
{
    [Serializable, BaseName("CaptaTrader")]
    public class AppData
    {
        public string? Email { get; set; }

        public string? NomicsAPIKeyEncrypted { get; set; }
        public string? BinanceAPIKeyEncrypted { get; set; }
        public string? BinanceSecretAPIKeyEncrypted { get; set; }

        public async Task SetNomicsAPIKeyAsync(string input, string passPhrase)
        {
            if (string.IsNullOrEmpty(input))
            {
                NomicsAPIKeyEncrypted = input;
            }
            else
            {
                NomicsAPIKeyEncrypted = await input.EncryptSimpleAsync(passPhrase);
            }
        }

        public async Task SetBinanceAPIKeyAsync(string input, string passPhrase)
        {
            if (string.IsNullOrEmpty(input))
            {
                BinanceAPIKeyEncrypted = input;
            }
            else
            {
                BinanceAPIKeyEncrypted = await input.EncryptSimpleAsync(passPhrase);
            }
        }

        public async Task SetBinanceSecretAPIKeyAsync(string input, string passPhrase)
        {
            if (string.IsNullOrEmpty(input))
            {
                BinanceSecretAPIKeyEncrypted = input;
            }
            else
            {
                BinanceSecretAPIKeyEncrypted = await input.EncryptSimpleAsync(passPhrase);
            }
        }
    }
}
