using CaptaTrader.Business.Models;

namespace CaptaTrader.Business.Base
{
    public interface ISessionValueProvider
    {
        EndUser? GetUser();
        void SetUser(EndUser user);
    }
}
