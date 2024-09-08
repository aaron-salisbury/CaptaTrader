using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Data.Database;
using CaptaTrader.Models;

namespace CaptaTrader.Business.Services
{
    public class SessionValueResolver : ISessionValueResolver
    {
        private readonly ApplicationDbContext _context;
        private readonly ISessionValueProvider _sessionValueProvider;

        public SessionValueResolver(ApplicationDbContext context, ISessionValueProvider sessionValueProvider)
        {
            _context = context;
            _sessionValueProvider = sessionValueProvider;
        }

        public EndUser? GetSessionUser(params string[] relationships)
        {
            int? endUserId = _sessionValueProvider.GetUserID();
            if (endUserId == null)
            {
                return null;
            }

            return _context.GetEntityByKey<EndUser>(endUserId.Value, relationships);
        }

        public void SetSessionUser(EndUser user)
        {
            _sessionValueProvider.SetUserID(user.Id);
        }
    }
}
