using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.UserAccess;

public interface IUserAccountStore
{
    Task<UserAccount?> GetAsync(CancellationToken cancellationToken = default);

    /// <returns><see langword="false"/> when an account already exists.</returns>
    Task<bool> TryCreateAsync(UserAccount account, CancellationToken cancellationToken = default);
}
