using CaptaTrader.Business.Models;
using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Data;

namespace CaptaTrader.Business.Services
{
    public class SessionValueResolver : ISessionValueResolver
    {
        private readonly IDataAccess _dataAccessor;
        private readonly ISessionValueProvider _sessionValueProvider;

        public SessionValueResolver(IDataAccess dataAccessor, ISessionValueProvider sessionValueProvider)
        {
            _dataAccessor = dataAccessor;
            _sessionValueProvider = sessionValueProvider;
        }

        public EndUser? GetSessionUser()
        {
            int? endUserId = _sessionValueProvider.GetUserID();
            if (endUserId == null)
            {
                return null;
            }

            return _dataAccessor.Read<EndUser>(endUserId.Value);
        }

        public void SetSessionUser(EndUser user)
        {
            _sessionValueProvider.SetUserID(user.Id);
        }
    }
}
