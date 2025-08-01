using RunnethOverStudio.AppToolkit.Modules.UserAccess;

namespace CaptaTrader.Business.Modules.UserAccess.DTOs;

public record EndUserDto
{
    public int EndUserId { get; init; }

    public required string UserName { get; init; }

    public UserCredentials? UserCredential { get; set; }

    public UserConfigDto? UserConfig { get; set; }
}
