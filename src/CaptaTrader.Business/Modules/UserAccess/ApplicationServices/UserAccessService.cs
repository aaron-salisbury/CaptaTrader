using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Modules.UserAccess.DTOs;
using CaptaTrader.Data.Entities;
using CaptaTrader.Data.Services;
using FluentValidation;
using FluentValidation.Results;
using RunnethOverStudio.AppToolkit.Core;
using RunnethOverStudio.AppToolkit.Modules.UserAccess;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.UserAccess.ApplicationServices;

public sealed class UserAccessService : IUserAccessService
{
    private readonly IUserAuthenticator _userAuthenticator;
    private readonly IDataAccess _dataAccess;
    private readonly IValidator<UserConfigDto> _userConfigValidator;
    private readonly UserAccessMapper _userAccessMapper = new();

    public UserAccessService(IUserAuthenticator userAuthenticator, IDataAccess dataAccess, IValidator<UserConfigDto> userConfigValidator)
    {
        _userAuthenticator = userAuthenticator;
        _dataAccess = dataAccess;
        _userConfigValidator = userConfigValidator;
    }

    public ProcessResult Authenticate(NetworkCredential networkCredentials)
    {
        EndUser? user = _dataAccess.ReadFiltered<EndUser>(u => string.Equals(networkCredentials.UserName, u.UserName), [nameof(EndUser.UserCredential)]).FirstOrDefault();
        if (user == null)
        {
            return new ProcessResult([new Exception("User Name not found.")]);
        }

        if (user.UserCredential == null || !_userAuthenticator.VerifyCredentials(_userAccessMapper.MapToDto<UserCredentials>(user.UserCredential), networkCredentials.SecurePassword))
        {
            return new ProcessResult([new Exception("Bad credentials.")]);
        }

        TradingBot.Instance.SessionEndUserId = user.EndUserId;
        return ProcessResult.Success();
    }

    public ProcessResult Register(NetworkCredential networkCredentials)
    {
        if (string.IsNullOrEmpty(networkCredentials.UserName) || _dataAccess.ReadFiltered<EndUser>(eu => string.Equals(networkCredentials.UserName, eu.UserName)).Any())
        {
            return new ProcessResult([new Exception("User Name must be unique.")]);
        }

        EndUserDto newUser = new()
        {
            UserName = networkCredentials.UserName,
            UserCredential = _userAuthenticator.CreateUserCredentials(networkCredentials.SecurePassword),
            UserConfig = new()
        };

        EndUser userEntity = _dataAccess.Update(_userAccessMapper.MapToEntity<EndUser>(newUser));

        TradingBot.Instance.SessionEndUserId = userEntity.EndUserId;
        return ProcessResult.Success();
    }

    public UserConfigDto? GetSessionUserConfig()
    {
        return GetSessionUser(nameof(EndUserDto.UserConfig))?.UserConfig;
    }

    public async Task<ProcessResult> SaveSessionUserConfigAsync(UserConfigDto userConfig)
    {
        ValidationResult result = await _userConfigValidator.ValidateAsync(userConfig);

        if (result.IsValid)
        {
            _dataAccess.Update(_userAccessMapper.MapToEntity<UserConfig>(userConfig));
        }

        return result.ToProcessResult();
    }

    public EndUserDto? GetSessionUser(params string[] relationships)
    {
        int? sessionEndUserId = GetSessionEndUserId();

        if (sessionEndUserId == null)
        {
            return null;
        }

        EndUser? endUser = _dataAccess.ReadFiltered<EndUser>(eu => eu.EndUserId == sessionEndUserId.Value, relationships).SingleOrDefault();
        if (endUser == null)
        {
            return null;
        }

        return _userAccessMapper.MapToDto<EndUserDto>(endUser);
    }

    public int? GetSessionEndUserId() => TradingBot.Instance.SessionEndUserId;
}
