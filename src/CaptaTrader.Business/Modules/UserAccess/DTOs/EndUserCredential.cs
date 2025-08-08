using System;

namespace CaptaTrader.Business.Modules.UserAccess.DTOs;

/// <summary>
/// Represents a data transfer object containing cryptographic credential information.
/// </summary>
public record EndUserCredential
{
    /// <summary>
    /// Gets or sets the unique identifier of the associated end user.
    /// </summary>
    public long? EndUserId { get; set; }

    /// <summary>
    /// Cryptographic salt used for hashing the user's login credentials.
    /// </summary>
    public required byte[] LoginSalt { get; init; }

    /// <summary>
    /// Hashed value of the user's login credentials.
    /// </summary>
    public required byte[] LoginHash { get; init; }

    /// <summary>
    /// Work factor (e.g., cost parameter) used in the password hashing algorithm.
    /// </summary>
    public required int LoginWorkFactor { get; init; }
}
