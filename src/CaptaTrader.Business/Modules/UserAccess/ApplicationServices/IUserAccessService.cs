using CaptaTrader.Business.Modules.UserAccess.DTOs;
using RunnethOverStudio.AppToolkit.Core;
using System.Net;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.UserAccess.ApplicationServices;

public interface IUserAccessService
{
    ProcessResult Authenticate(NetworkCredential networkCredentials);

    ProcessResult Register(NetworkCredential networkCredentials);

    UserConfigDto? GetSessionUserConfig();

    Task<ProcessResult> SaveSessionUserConfigAsync(UserConfigDto userConfig);

    EndUserDto? GetSessionUser(params string[] relationships);

    int? GetSessionEndUserId();
}
