using CaptaTrader.Business.Base.Extensions;
using CaptaTrader.Business.Modules.UserAccess.DTOs;
using CaptaTrader.Data.Services;
using FluentValidation;
using FluentValidation.Results;
using RunnethOverStudio.AppToolkit.Core;
using RunnethOverStudio.AppToolkit.Modules.UserAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CaptaTrader.Business.Modules.UserAccess.ApplicationServices;

public sealed class UserAccessService : IUserAccessService
{
    private readonly IUserAuthenticator _userAuthenticator;
    private readonly ISQLDataAccess _dataAccess;
    private readonly IValidator<EndUserConfig> _userConfigValidator;

    public UserAccessService(IUserAuthenticator userAuthenticator, ISQLDataAccess dataAccess, IValidator<EndUserConfig> userConfigValidator)
    {
        _userAuthenticator = userAuthenticator;
        _dataAccess = dataAccess;
        _userConfigValidator = userConfigValidator;
    }

    public async Task<ProcessResult> AuthenticateAsync(NetworkCredential networkCredentials)
    {
        //TODO: Need to be able to support relationships/joins in order to do this all in one operation.
        EndUser? endUser = (await _dataAccess.ReadAsync<EndUser>("Username = @Username", new { Username = networkCredentials.UserName }, "EndUserId")).FirstOrDefault();
        if (endUser?.EndUserId == null)
        {
            return new ProcessResult([new Exception("User not found for username.")]);
        }

        EndUserCredential? endUserCredentialEntity = (await _dataAccess.ReadAsync<EndUserCredential>("EndUserId = @EndUserId", new { EndUserId = endUser.EndUserId.Value })).FirstOrDefault();
        if (endUserCredentialEntity == null)
        {
            return new ProcessResult([new Exception("Credentials not found for user.")]);
        }

        //TODO: Won't need this after EndUserCredential in the ROS library gets included in the next NuGet version.
        UserCredentials userCredentials = new()
        {
            LoginHash = (byte[])endUserCredentialEntity.LoginHash,
            LoginSalt = (byte[])endUserCredentialEntity.LoginSalt,
            LoginWorkFactor = (int)endUserCredentialEntity.LoginWorkFactor
        };

        if (!_userAuthenticator.VerifyCredentials(userCredentials, networkCredentials.SecurePassword))
        {
            return new ProcessResult([new Exception("Invalid credentials.")]);
        }

        TradingBot.Instance.SessionEndUserId = endUser.EndUserId;
        return ProcessResult.Success();
    }

    public async Task<ProcessResult> RegisterAsync(NetworkCredential networkCredentials)
    {
        // Check if the user name is provided and unique.
        if (string.IsNullOrEmpty(networkCredentials.UserName))
        {
            return new ProcessResult([new Exception("Username is required.")]);
        }

        IEnumerable<EndUser> existingUsers = await _dataAccess.ReadAsync<EndUser>("Username = @Username", new { Username = networkCredentials.UserName });
        if (existingUsers.Any())
        {
            return new ProcessResult([new Exception("Username must be unique.")]);
        }

        // Create a new user with the provided user name.
        long? newUserId = await _dataAccess.CreateAsync(new EndUser() { Username = networkCredentials.UserName });
        if (newUserId == null)
        {
            return new ProcessResult([new Exception("Failed to create user.")]);
        }

        // Create user credentials with the provided password.
        //TODO: EndUserCredential should be in the ROS library.
        UserCredentials userCredential = _userAuthenticator.CreateUserCredentials(networkCredentials.SecurePassword);
        EndUserCredential endUserCredential = new()
        {
            EndUserId = newUserId,
            LoginSalt = userCredential.LoginSalt,
            LoginHash = userCredential.LoginHash,
            LoginWorkFactor = userCredential.LoginWorkFactor
        };

        long? endUserCredentialId = await _dataAccess.CreateAsync(endUserCredential);
        if (endUserCredentialId == null)
        {
            await _dataAccess.DeleteByPrimaryKeyAsync<EndUser>(newUserId.Value);
            return new ProcessResult([new Exception("Failed to create user credentials. User creation rolled back.")]);
        }

        TradingBot.Instance.SessionEndUserId = newUserId;
        return ProcessResult.Success();
    }

    public async Task<EndUserConfig?> GetSessionUserConfigAsync()
    {
        long? endUserId = GetSessionEndUserId();
        if (endUserId == null)
        {
            return null;
        }

        return (await _dataAccess.ReadAsync<EndUserConfig>("EndUserId = @EndUserId", new { EndUserId = endUserId.Value }))
            .FirstOrDefault();
    }

    public async Task<ProcessResult> SaveSessionUserConfigAsync(EndUserConfig userConfig)
    {
        ValidationResult result = await _userConfigValidator.ValidateAsync(userConfig);

        if (result.IsValid)
        {
            Dictionary<string, object?> columnsAndValues = new()
            {
                { "Email", userConfig.Email },
                { "MarketDataAPI", (int?)userConfig.MarketDataAPI },
                { "MarketDataAPIKey", userConfig.MarketDataAPIKey },
                { "ExchangeAPI", (int?)userConfig.ExchangeAPI },
                { "ExchangeAPIKey", userConfig.ExchangeAPIKey },
                { "ExchangeSecretKey", userConfig.ExchangeSecretKey }
            };

            bool updated = await _dataAccess.UpdateByPrimaryKeyAsync<EndUserConfig>(userConfig.EndUserConfigId, columnsAndValues);
            if (!updated)
            {
                return new ProcessResult([new Exception("Failed to update user configuration.")]);
            }
        }

        return result.ToProcessResult();
    }

    public long? GetSessionEndUserId() => TradingBot.Instance.SessionEndUserId;
}
