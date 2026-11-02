using System;

namespace CaptaTrader.Business.Modules.UserAccess;

/// <summary>
/// The single local login. CaptaTrader is one-to-one with its user, so at most one account exists.
/// </summary>
public sealed record UserAccount(string UserName, PasswordHash Password, DateTimeOffset CreatedAtUtc);

/// <summary>
/// Salted PBKDF2 password hash. Stores the iteration count so the work factor can rise later
/// without invalidating existing logins.
/// </summary>
public sealed record PasswordHash(byte[] Hash, byte[] Salt, int Iterations);
