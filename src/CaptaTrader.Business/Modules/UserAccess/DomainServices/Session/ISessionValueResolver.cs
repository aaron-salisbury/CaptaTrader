using CaptaTrader.Business.Modules.UserAccess.DTOs;

namespace CaptaTrader.Business.Modules.UserAccess.DomainServices.Session;

// Works in conjunction with ISessionValueProvider so that session data doesn't need to hold on to everything in the heap.
public interface ISessionValueResolver
{
    EndUserDto? GetSessionUser(params string[] relationships);

    void SetSessionUserID(int userId);
}
