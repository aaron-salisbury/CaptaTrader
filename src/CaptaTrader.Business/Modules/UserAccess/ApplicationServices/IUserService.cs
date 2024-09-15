using CaptaTrader.Business.Modules.UserAccess.DTOs;
using FluentValidation.Results;
using System.Net;

namespace CaptaTrader.Business.Modules.UserAccess.ApplicationServices;

public interface IUserService
{
    ValidationResult Authenticate(NetworkCredential networkCredentials);

    ValidationResult Register(NetworkCredential networkCredentials);

    EndUserDto? GetEndUser(int? id, params string[] relationships);

    Task<ValidationResult> SaveEndUserAsync(EndUserDto user);
}
