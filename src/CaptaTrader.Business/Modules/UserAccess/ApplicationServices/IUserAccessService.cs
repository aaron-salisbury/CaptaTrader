using CaptaTrader.Business.Modules.UserAccess.DTOs;
using FluentValidation.Results;
using System.Net;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.UserAccess.ApplicationServices;

public interface IUserAccessService
{
    ValidationResult Authenticate(NetworkCredential networkCredentials);

    ValidationResult Register(NetworkCredential networkCredentials);

    UserConfigDto? GetSessionUserConfig();

    Task<ValidationResult> SaveSessionUserConfigAsync(UserConfigDto userConfig);

    EndUserDto? GetSessionUser(params string[] relationships);

    int? GetSessionEndUserId();
}
