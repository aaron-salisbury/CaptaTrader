using System.Security;

namespace CaptaTrader.Business.Services
{
    internal interface IKeyVaultService
    {
        SecureString? GetValueForKey(string key);
    }
}
