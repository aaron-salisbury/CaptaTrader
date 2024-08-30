using CaptaTrader.Business.Models;
using CaptaTrader.Data.Base.Extensions;
using System.Security;
using System.Security.Cryptography;

namespace CaptaTrader.Business.Base
{
    // https://www.mking.net/blog/password-security-best-practices-with-examples-in-csharp

    public static class Security
    {
        private const int SALT_LENGTH = 32;
        private const int NEW_USER_WORK_FACTOR = 10000;

        public static bool Login(AppCredentials credentials, SecureString password)
        {
            return credentials.LoginHash.EqualsByteArray(GenerateHash(
                password.ToBytes(),
                credentials.LoginSalt,
                credentials.LoginWorkFactor));
        }

        public static AppCredentials CreateAppCredentials(SecureString password)
        {
            byte[] loginSalt = GenerateSalt();

            return new AppCredentials()
            {
                LoginSalt = loginSalt,
                LoginHash = GenerateHash(password.ToBytes(), loginSalt, NEW_USER_WORK_FACTOR),
                LoginWorkFactor = NEW_USER_WORK_FACTOR
            };
        }

        private static byte[] GenerateSalt()
        {
            var bytes = new byte[SALT_LENGTH];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return bytes;
        }

        private static byte[] GenerateHash(byte[] password, byte[] salt, int workFactor)
        {
            using (Rfc2898DeriveBytes deriveBytes = new(password, salt, workFactor, HashAlgorithmName.SHA1))
            {
                return deriveBytes.GetBytes(SALT_LENGTH);
            }
        }
    }
}
