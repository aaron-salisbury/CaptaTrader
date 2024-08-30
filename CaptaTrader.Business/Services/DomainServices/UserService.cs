using CaptaTrader.Business.Base;
using CaptaTrader.Business.Models;
using CaptaTrader.Data;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CaptaTrader.Business.Services
{
    public class UserService : IUserService
    {
        private DataAccesor _dataAccesor;
        private ISessionValueProvider _sessionValueProvider;

        public UserService(DataAccesor dataAccesor, ISessionValueProvider sessionValueProvider)
        {
            _dataAccesor = dataAccesor;
            _sessionValueProvider = sessionValueProvider;
        }

        public (EndUser?, IEnumerable<ValidationResult>) Authenticate(NetworkCredential networkCredentials)
        {
            //TODO:
            // https://jasonwatmore.com/post/2021/12/20/net-6-basic-authentication-tutorial-with-example-api
            // https://github.com/cornflourblue/dotnet-6-registration-login-api/blob/master/Controllers/UsersController.cs

            EndUser? user = _dataAccesor.ReadWhere<EndUser>(u => string.Equals(networkCredentials.UserName, u.UserName)).FirstOrDefault();
            if (user == null)
            {
                return (null, [new ValidationResult("User Name not found.")]);
            }

            AppCredentials? appCredentials = _dataAccesor.ReadWhere<AppCredentials>(ac => user.Id == ac.UserId).FirstOrDefault();
            if (appCredentials == null || !Security.Login(appCredentials, networkCredentials.SecurePassword))
            {
                return (null, [new ValidationResult("Bad credentials.")]);
            }

            _sessionValueProvider.SetUser(user);
            return (user, []);
        }

        public (EndUser?, IEnumerable<ValidationResult>) Register(NetworkCredential networkCredentials)
        {
            if (string.IsNullOrEmpty(networkCredentials.UserName) || _dataAccesor.ReadAll<EndUser>().Any(u => string.Equals(networkCredentials.UserName, u.UserName)))
            {
                return (null, [new ValidationResult("User Name must be unique.")]);
            }

            EndUser newUser = new() { UserName = networkCredentials.UserName };
            IEnumerable<ValidationResult> userModelResults = newUser.Validate();
            if (userModelResults.Any())
            {
                return (null, userModelResults);
            }
            _dataAccesor.Create(newUser);

            AppCredentials appCredentials = Security.CreateAppCredentials(networkCredentials.SecurePassword);
            appCredentials.UserId = newUser.Id;
            _dataAccesor.Create(appCredentials);

            _sessionValueProvider.SetUser(newUser);
            return (newUser, []);
        }

        public IEnumerable<ValidationResult> Save(EndUser user)
        {
            IEnumerable<ValidationResult> results = user.Validate();

            if (!results.Any())
            {
                _dataAccesor.Update(user);
            }
            
            return results;
        }
    }
}
