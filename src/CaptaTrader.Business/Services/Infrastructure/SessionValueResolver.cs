using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Data;
using CaptaTrader.Domains;

namespace CaptaTrader.Business.Services;

public class SessionValueResolver : ISessionValueResolver
{
    private readonly IDataAccess _dataAccess;
    private readonly ISessionValueProvider _sessionValueProvider;

    public SessionValueResolver(IDataAccess dataAccess, ISessionValueProvider sessionValueProvider)
    {
        _dataAccess = dataAccess;
        _sessionValueProvider = sessionValueProvider;
    }

    public EndUser? GetSessionUser(params string[] relationships)
    {
        int? endUserId = _sessionValueProvider.GetUserID();
        if (endUserId == null)
        {
            return null;
        }

        return _dataAccess.ReadFiltered<EndUser>((eu => eu.EndUserId == endUserId), relationships).SingleOrDefault();
    }

    public void SetSessionUser(EndUser user)
    {
        _sessionValueProvider.SetUserID(user.EndUserId);
    }
}
