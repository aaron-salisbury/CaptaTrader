using System;
using System.Security.Cryptography;
using System.Text;

namespace CaptaTrader.Business.Modules.UserAccess;

/// <summary>
/// Hashes and verifies the local login password.
/// </summary>
/// <remarks>
/// Parameters follow the OWASP Password Storage Cheat Sheet recommendation for PBKDF2-HMAC-SHA256
/// (600,000 iterations): <see href="https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html"/>.
/// </remarks>
internal static class PasswordHasher
{
    internal const int ITERATIONS = 600_000;
    private const int SALT_BYTES = 16;
    private const int HASH_BYTES = 32;

    internal static PasswordHash Hash(string password, int iterations = ITERATIONS)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        byte[] salt = RandomNumberGenerator.GetBytes(SALT_BYTES);
        byte[] hash = Derive(password, salt, iterations);

        return new PasswordHash(hash, salt, iterations);
    }

    internal static bool Verify(string password, PasswordHash stored)
    {
        ArgumentNullException.ThrowIfNull(stored);

        if (string.IsNullOrEmpty(password))
        {
            return false;
        }

        byte[] candidate = Derive(password, stored.Salt, stored.Iterations);

        try
        {
            return CryptographicOperations.FixedTimeEquals(candidate, stored.Hash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(candidate);
        }
    }

    private static byte[] Derive(string password, byte[] salt, int iterations)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        try
        {
            return Rfc2898DeriveBytes.Pbkdf2(passwordBytes, salt, iterations, HashAlgorithmName.SHA256, HASH_BYTES);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }
}
