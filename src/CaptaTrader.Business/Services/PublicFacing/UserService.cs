using CaptaTrader.Business.Base;
using CaptaTrader.Data;
using CaptaTrader.Domains;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CaptaTrader.Business.Services.Domain;

public class UserService : IUserService
{
    private readonly IDataAccess _dataAccess;
    private readonly ISessionValueResolver _sessionValueResolver;

    public UserService(IDataAccess dataAccess, ISessionValueResolver sessionValueResolver)
    {
        _dataAccess = dataAccess;
        _sessionValueResolver = sessionValueResolver;
    }

    public EndUser CreateNewDefaultUser()
    {
        EndUser newUser = new() { UserName = Environment.UserName };

        return _dataAccess.Create(newUser);
    }

    public IEnumerable<ValidationResult> Authenticate(NetworkCredential networkCredentials)
    {
        EndUser? user = _dataAccess.ReadFiltered<EndUser>((u => string.Equals(networkCredentials.UserName, u.UserName)), [nameof(EndUser.AppCredential)]).FirstOrDefault();
        if (user == null)
        {
            return [new ValidationResult("User Name not found.")];
        }

        AppCredential? userAppCredential = user.AppCredential;
        if (userAppCredential == null || !Security.Login(userAppCredential, networkCredentials.SecurePassword))
        {
            return [new ValidationResult("Bad credentials.")];
        }

        _sessionValueResolver.SetSessionUser(user);
        return [];
    }

    public IEnumerable<ValidationResult> Register(NetworkCredential networkCredentials)
    {
        if (string.IsNullOrEmpty(networkCredentials.UserName) || _dataAccess.ReadFiltered<EndUser>(eu => string.Equals(networkCredentials.UserName, eu.UserName)).Any())
        {
            return [new ValidationResult("User Name must be unique.")];
        }

        EndUser newUser = CreateNewDefaultUser();
        newUser.UserName = networkCredentials.UserName;
        newUser.AppCredential = Security.CreateAppCredential(networkCredentials.SecurePassword);

        IEnumerable<ValidationResult> validationResults = newUser.Validate();
        if (validationResults.Any())
        {
            _dataAccess.Delete(newUser);
            return validationResults;
        }

        newUser = _dataAccess.Update(newUser);

        _sessionValueResolver.SetSessionUser(newUser);
        return [];
    }

    public EndUser? GetEndUser(int? id, params string[] relationships)
    {
        if (id == null)
        {
            return null;
        }

        return _dataAccess.ReadFiltered<EndUser>((eu => eu.EndUserId == id.Value), relationships).SingleOrDefault();
    }

    public IEnumerable<ValidationResult> SaveEndUser(EndUser user)
    {
        IEnumerable<ValidationResult> results = user.Validate();

        if (!results.Any())
        {
            _dataAccess.Update(user);
        }
        
        return results;
    }
}
