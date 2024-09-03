using CaptaTrader.Business.Base;
using CaptaTrader.Business.Models;
using CaptaTrader.Data;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CaptaTrader.Business.Services
{
    public class UserService : IUserService
    {
        private AppDataAccessor _dataAccessor;
        private ISessionValueProvider _sessionValueProvider;

        public UserService(AppDataAccessor dataAccessor, ISessionValueProvider sessionValueProvider)
        {
            _dataAccessor = dataAccessor;
            _sessionValueProvider = sessionValueProvider;
        }

        public IEnumerable<ValidationResult> Authenticate(NetworkCredential networkCredentials)
        {
            EndUser? user = _dataAccessor.ReadWhere<EndUser>(u => string.Equals(networkCredentials.UserName, u.UserName)).FirstOrDefault();
            if (user == null)
            {
                return [new ValidationResult("User Name not found.")];
            }

            AppCredentials? credentials = _dataAccessor.ReadWhere<AppCredentials>(ac => user.Id == ac.EndUserId).FirstOrDefault();
            if (credentials == null || !Security.Login(credentials, networkCredentials.SecurePassword))
            {
                return [new ValidationResult("Bad credentials.")];
            }

            _sessionValueProvider.SetUser(user);
            return [];
        }

        public IEnumerable<ValidationResult> Register(NetworkCredential networkCredentials)
        {
            if (string.IsNullOrEmpty(networkCredentials.UserName) || _dataAccessor.ReadAll<EndUser>().Any(u => string.Equals(networkCredentials.UserName, u.UserName)))
            {
                return [new ValidationResult("User Name must be unique.")];
            }

            EndUser newUser = new() { UserName = networkCredentials.UserName };
            IEnumerable<ValidationResult> userModelResults = newUser.Validate();
            if (userModelResults.Any())
            {
                return userModelResults;
            }
            _dataAccessor.Create(newUser);

            AppCredentials credentials = Security.CreateUserCredentials(networkCredentials.SecurePassword);
            credentials.EndUserId = newUser.Id;
            _dataAccessor.Create(credentials);

            _sessionValueProvider.SetUser(newUser);
            return [];
        }

        public IEnumerable<ValidationResult> Save(EndUser user)
        {
            IEnumerable<ValidationResult> results = user.Validate();

            if (!results.Any())
            {
                _dataAccessor.Update(user);
            }
            
            return results;
        }
    }
}
