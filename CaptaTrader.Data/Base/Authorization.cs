using CaptaTrader.Models;
using System.Security.Cryptography;

namespace CaptaTrader.Data.Base
{
    // https://www.mking.net/blog/password-security-best-practices-with-examples-in-csharp

    public static class Authorization
    {
        private const int SHA_BYTE_LENGTH = 32;
        private const int NEW_USER_WORK_FACTOR = 10000;

        public static bool Login(User user, byte[] password)
        {
            return user.LoginHash == GenerateHash(password, user.LoginSalt, user.LoginWorkFactor);
        }

        public static User CreateAuthorizedUser(byte[] password)
        {
            byte[] loginSalt = GenerateSalt();

            return new User()
            {
                LoginSalt = loginSalt,
                LoginHash = GenerateHash(password, loginSalt, NEW_USER_WORK_FACTOR),
                LoginWorkFactor = NEW_USER_WORK_FACTOR
            };
        }

        private static byte[] GenerateSalt()
        {
            var bytes = new byte[SHA_BYTE_LENGTH];

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
                return deriveBytes.GetBytes(SHA_BYTE_LENGTH);
            }
        }
    }
}
