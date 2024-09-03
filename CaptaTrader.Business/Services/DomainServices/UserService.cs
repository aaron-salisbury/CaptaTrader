using CaptaTrader.Business.Base;
using CaptaTrader.Business.Models;
using CaptaTrader.Data;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CaptaTrader.Business.Services
{
    public class UserService : IUserService
    {
        private IDataAccess _dataAccessor;
        private ISessionValueProvider _sessionValueProvider;

        public UserService(IDataAccess dataAccessor, ISessionValueProvider sessionValueProvider)
        {
            _dataAccessor = dataAccessor;
            _sessionValueProvider = sessionValueProvider;
        }

        public EndUser CreateNewDefaultUser()
        {
            string defaultUserName = Environment.UserName;

            EndUser newUser = new() { UserName = defaultUserName };

            _dataAccessor.Create(newUser);

            return newUser;
        }

        public IEnumerable<ValidationResult> Authenticate(NetworkCredential networkCredentials)
        {
            EndUser? user = _dataAccessor.ReadWhere<EndUser>(u => string.Equals(networkCredentials.UserName, u.UserName)).FirstOrDefault();
            if (user == null)
            {
                return [new ValidationResult("User Name not found.")];
            }

            AppCredentials? credentials = user.AppCredentials;
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

            EndUser newUser = CreateNewDefaultUser();
            newUser.UserName = networkCredentials.UserName;
            newUser.AppCredentials = Security.CreateAppCredentials(networkCredentials.SecurePassword);

            IEnumerable<ValidationResult> validationResults = newUser.Validate();
            if (validationResults.Any())
            {
                _dataAccessor.Delete(newUser);
                return validationResults;
            }

            _dataAccessor.Update(newUser);

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
