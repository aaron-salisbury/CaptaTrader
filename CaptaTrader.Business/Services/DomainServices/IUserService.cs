using CaptaTrader.Business.Models;
using System.Net;

namespace CaptaTrader.Business.Services
{
    public interface IUserService
    {
        EndUser? Authenticate(NetworkCredential networkCredentials);

        EndUser? Register(NetworkCredential networkCredentials);

        void Save(EndUser user);
    }
}
