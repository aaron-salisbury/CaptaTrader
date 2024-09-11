using CaptaTrader.Models;

namespace CaptaTrader.Business.Services;

// Works in conjunction with ISessionValueProvider so that session data doesn't need to hold on to everything in the heap.
public interface ISessionValueResolver
{
    EndUser? GetSessionUser(params string[] relationships);

    void SetSessionUser(EndUser user);
}
