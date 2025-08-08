using System;

namespace CaptaTrader.Business.Modules.UserAccess.DTOs;

public record EndUser
{
    public long? EndUserId { get; init; }

    public required string Username { get; init; }
}
