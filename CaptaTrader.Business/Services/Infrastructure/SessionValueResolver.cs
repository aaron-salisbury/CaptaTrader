using CaptaTrader.Business.Services.Domain;
using CaptaTrader.Data.Database;
using CaptaTrader.Models;
using Microsoft.EntityFrameworkCore;

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

            return _context.GetEndUser(endUserId.Value, relationships);
        }

        private IQueryable<T> Something<T>(IQueryable<T> previousQueryable, Stack<string> relationships) where T : class
        {
            string relationship = relationships.Pop();

            IQueryable<T> newQueryable = previousQueryable.Include(relationship);

            if (relationships.Count != 0)
            {
                return Something(newQueryable, relationships);
            }
            else
            {
                return newQueryable;
            }
        }

        public void SetSessionUser(EndUser user)
        {
            _sessionValueProvider.SetUserID(user.Id);
        }
    }
}
