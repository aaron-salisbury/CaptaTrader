using CaptaTrader.Business.Base;
using CaptaTrader.Business.Models;
using CaptaTrader.Data;
using System.Net;

namespace CaptaTrader.Business.Services
{
    public class UserService : IUserService
    {
        private DataAccesor _dataAccesor;

        public UserService(DataAccesor dataAccesor)
        {
            _dataAccesor = dataAccesor;
        }

        public EndUser? Authenticate(NetworkCredential networkCredentials)
        {
            //TODO:
            // https://jasonwatmore.com/post/2021/12/20/net-6-basic-authentication-tutorial-with-example-api
            // https://github.com/cornflourblue/dotnet-6-registration-login-api/blob/master/Controllers/UsersController.cs

            EndUser? user = _dataAccesor.ReadWhere<EndUser>(u => string.Equals(networkCredentials.UserName, u.UserName)).FirstOrDefault();
            if (user == null)
            {
                return null;
            }

            AppCredentials? appCredentials = _dataAccesor.ReadWhere<AppCredentials>(ac => user.Id == ac.UserId).FirstOrDefault();
            if (appCredentials != null && Security.Login(appCredentials, networkCredentials.SecurePassword))
            {
                Startup.SessionUser = user;
                return user;
            }

            return null;
        }

        public EndUser? Register(NetworkCredential networkCredentials)
        {
            if (string.IsNullOrEmpty(networkCredentials.UserName) || _dataAccesor.ReadAll<EndUser>().Any(u => string.Equals(networkCredentials.UserName, u.UserName)))
            {
                return null;
            }

            EndUser newUser = new() { UserName = networkCredentials.UserName };
            _dataAccesor.Create(newUser);

            AppCredentials appCredentials = Security.CreateAppCredentials(networkCredentials.SecurePassword);
            appCredentials.UserId = newUser.Id;
            _dataAccesor.Create(appCredentials);

            Startup.SessionUser = newUser;
            return newUser;
        }

        public void Save(EndUser user)
        {
            _dataAccesor.Update(user);
        }
    }
}
