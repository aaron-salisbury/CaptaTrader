using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace CaptaTrader.Business.Base.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Convert to SecureString.
        /// </summary>
        public static SecureString ToSecureString(this string plainString)
        {
            SecureString secureString = new(); 
            secureString.Clear();

            foreach (char c in plainString.ToCharArray())
            {
                secureString.AppendChar(c);
            }

            return secureString;
        }

        /// <summary>
        /// Encrypt using a pass phrase.
        /// </summary>
        internal static async Task<string> EncryptSimpleAsync(this string input, SecureString passPhrase)
        {
            // code-maze.com/csharp-string-encryption-decryption/

            using (Aes aes = GetAesSymmetricAlgorithm(passPhrase))
            using (MemoryStream outputStream = new())
            using (CryptoStream cryptoStream = new(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                await cryptoStream.WriteAsync(Encoding.Unicode.GetBytes(input));
                await cryptoStream.FlushFinalBlockAsync();

                return Convert.ToBase64String(outputStream.ToArray());
            }
        }

        /// <summary>
        /// Decrypt using the pass phrase used when encrypted.
        /// </summary>
        internal static async Task<SecureString> DecryptSimpleAsync(this string input, SecureString passPhrase)
        {
            SecureString secured = new();

            if (string.IsNullOrEmpty(input))
            {
                return secured;
            }

            byte[] encryptedBytes = Convert.FromBase64String(input);

            using (Aes aes = GetAesSymmetricAlgorithm(passPhrase))
            using (MemoryStream inputSteam = new(encryptedBytes))
            using (CryptoStream cryptoStream = new(inputSteam, aes.CreateDecryptor(), CryptoStreamMode.Read))
            using (MemoryStream outputSteam = new())
            {
                await cryptoStream.CopyToAsync(outputSteam);

                return Encoding.Unicode.GetString(outputSteam.ToArray()).ToSecureString();
            }
        }

        private static Aes GetAesSymmetricAlgorithm(SecureString passPhrase)
        {
            Aes encryptor = Aes.Create();

            encryptor.IV = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16];

            encryptor.Key = Rfc2898DeriveBytes.Pbkdf2(Encoding.Unicode.GetBytes(passPhrase.ToPlainString()),
                                             Encoding.UTF8.GetBytes("4F24B54A-12ED-429E-BADE-79C1BDB57702"),
                                             1645, // Picked a random number between 1000 and 5000.
                                             HashAlgorithmName.SHA384,
                                             16); // 16 bytes equal 128 bits.

            return encryptor;
        }
    }
}
