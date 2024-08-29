using CaptaTrader.Business.Entities;
using System.Net;

namespace CaptaTrader.Business.Services
{
    public interface IUserService
    {
        User? Authenticate(NetworkCredential networkCredentials);

        User? Register(NetworkCredential networkCredentials);

        void Save(User user);
    }
}
