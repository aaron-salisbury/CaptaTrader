using CaptaTrader.Business.Base;
using CaptaTrader.Business.Entities;
using CaptaTrader.Data;
using System.Net;

namespace CaptaTrader.Business.Services
{
    public class UserService : IUserService
    {
        private DataAccesor _dataAccesor;

        internal UserService(DataAccesor dataAccesor)
        {
            _dataAccesor = dataAccesor;
        }

        public User? Authenticate(NetworkCredential networkCredentials)
        {
            //TODO:
            // https://jasonwatmore.com/post/2021/12/20/net-6-basic-authentication-tutorial-with-example-api
            // https://github.com/cornflourblue/dotnet-6-registration-login-api/blob/master/Controllers/UsersController.cs

            User? user = _dataAccesor.ReadWhere<User>(u => string.Equals(networkCredentials.UserName, u.UserName)).FirstOrDefault();
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

        public User? Register(NetworkCredential networkCredentials)
        {
            if (string.IsNullOrEmpty(networkCredentials.UserName) || _dataAccesor.ReadAll<User>().Any(u => string.Equals(networkCredentials.UserName, u.UserName)))
            {
                return null;
            }

            User newUser = new() { UserName = networkCredentials.UserName };
            _dataAccesor.Create(newUser);

            AppCredentials appCredentials = Security.CreateAppCredentials(networkCredentials.SecurePassword);
            appCredentials.UserId = newUser.Id;
            _dataAccesor.Create(appCredentials);

            Startup.SessionUser = newUser;
            return newUser;
        }

        public void Save(User user)
        {
            _dataAccesor.Update(user);
        }
    }
}
