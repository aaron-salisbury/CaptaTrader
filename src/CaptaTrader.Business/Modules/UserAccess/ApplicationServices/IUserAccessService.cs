using CaptaTrader.Business.Modules.UserAccess.DTOs;
using RunnethOverStudio.AppToolkit.Core;
using System.Net;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.UserAccess.ApplicationServices;

public interface IUserAccessService
{
    Task<ProcessResult> AuthenticateAsync(NetworkCredential networkCredentials);

    Task<ProcessResult> RegisterAsync(NetworkCredential networkCredentials);

    Task<EndUserConfig?> GetSessionUserConfigAsync();

    Task<ProcessResult> SaveSessionUserConfigAsync(EndUserConfig userConfig);

    long? GetSessionEndUserId();
}
