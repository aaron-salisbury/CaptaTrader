using System;

namespace CaptaTrader.Business.Modules.UserAccess.DTOs;

public record EndUserDto
{
    public int EndUserId { get; init; }

    public required string UserName { get; init; }

    public UserCredentialDto? UserCredential { get; set; }

    public UserConfigDto? UserConfig { get; set; }
}
