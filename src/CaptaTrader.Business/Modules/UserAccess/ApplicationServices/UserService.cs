using CaptaTrader.Business.Modules.UserAccess.DomainServices.Authorization;
using CaptaTrader.Business.Modules.UserAccess.DomainServices.Session;
using CaptaTrader.Business.Modules.UserAccess.DTOs;
using CaptaTrader.Data;
using CaptaTrader.Data.Entities;
using FluentValidation;
using FluentValidation.Results;
using System.Net;

namespace CaptaTrader.Business.Modules.UserAccess.ApplicationServices;

public class UserService : IUserService
{
    private readonly IDataAccess _dataAccess;
    private readonly ISessionValueResolver _sessionValueResolver;
    private IValidator<EndUserDto> _userValidator;
    private readonly UserAccessMapper _userAccessMapper = new();

    public UserService(IDataAccess dataAccess, ISessionValueResolver sessionValueResolver, IValidator<EndUserDto> userValidator)
    {
        _dataAccess = dataAccess;
        _sessionValueResolver = sessionValueResolver;
        _userValidator = userValidator;
}

    public ValidationResult Authenticate(NetworkCredential networkCredentials)
    {
        EndUser? user = _dataAccess.ReadFiltered<EndUser>(u => string.Equals(networkCredentials.UserName, u.UserName), [nameof(EndUser.UserCredential)]).FirstOrDefault();
        if (user == null)
        {
            return new ValidationResult([new ValidationFailure() { ErrorMessage = "User Name not found." }]);
        }

        if (user.UserCredential == null || !Authorizer.Login(_userAccessMapper.MapToDto<UserCredentialDto>(user.UserCredential), networkCredentials.SecurePassword))
        {
            return new ValidationResult([new ValidationFailure() { ErrorMessage = "Bad credentials." }]);
        }

        _sessionValueResolver.SetSessionUserID(user.EndUserId);
        return new ValidationResult();
    }

    public ValidationResult Register(NetworkCredential networkCredentials)
    {
        if (string.IsNullOrEmpty(networkCredentials.UserName) || _dataAccess.ReadFiltered<EndUser>(eu => string.Equals(networkCredentials.UserName, eu.UserName)).Any())
        {
            return new ValidationResult([new ValidationFailure() { ErrorMessage = "User Name must be unique." }]);
        }

        EndUserDto newUser = new()
        {
            UserName = networkCredentials.UserName,
            UserCredential = Authorizer.CreateAppCredential(networkCredentials.SecurePassword),
            UserConfig = new()
        };

        EndUser userEntity = _dataAccess.Update(_userAccessMapper.MapToEntity<EndUser>(newUser));

        _sessionValueResolver.SetSessionUserID(userEntity.EndUserId);
        return new ValidationResult();
    }

    public EndUserDto? GetEndUser(int? id, params string[] relationships)
    {
        if (id != null && 
            _dataAccess.ReadFiltered<EndUser>(eu => eu.EndUserId == id.Value, relationships).FirstOrDefault() is EndUser user)
        {
            return _userAccessMapper.MapToDto<EndUserDto>(user);
        }

        return null;
    }

    public async Task<ValidationResult> SaveEndUserAsync(EndUserDto user)
    {
        ValidationResult result = await _userValidator.ValidateAsync(user);
        
        if (result.IsValid)
        {
            _dataAccess.Update(user);
        }

        return result;
    }
}
