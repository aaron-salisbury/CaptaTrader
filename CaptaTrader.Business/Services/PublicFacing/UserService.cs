using CaptaTrader.Business.Base;
using CaptaTrader.Data.Database;
using CaptaTrader.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace CaptaTrader.Business.Services.Domain
{
    public class UserService : IUserService
    {
        private ApplicationDbContext _context;
        private ISessionValueResolver _sessionValueResolver;

        public UserService(ApplicationDbContext context, ISessionValueResolver sessionValueResolver)
        {
            _context = context;
            _sessionValueResolver = sessionValueResolver;
        }

        public EndUser CreateNewDefaultUser()
        {
            EndUser newUser = new(Environment.UserName);

            _context.SaveAndReload(_context.EndUsers.Add(newUser));

            return newUser;
        }

        public IEnumerable<ValidationResult> Authenticate(NetworkCredential networkCredentials)
        {
            EndUser? user = _context.EndUsers.Where(u => string.Equals(networkCredentials.UserName, u.UserName)).Include(eu => eu.AppCredentials).FirstOrDefault();
            if (user == null)
            {
                return [new ValidationResult("User Name not found.")];
            }

            AppCredentials? credentials = user.AppCredentials;
            if (credentials == null || !Security.Login(credentials, networkCredentials.SecurePassword))
            {
                return [new ValidationResult("Bad credentials.")];
            }

            _sessionValueResolver.SetSessionUser(user);
            return [];
        }

        public IEnumerable<ValidationResult> Register(NetworkCredential networkCredentials)
        {
            if (string.IsNullOrEmpty(networkCredentials.UserName) || _context.EndUsers.Any(u => string.Equals(networkCredentials.UserName, u.UserName)))
            {
                return [new ValidationResult("User Name must be unique.")];
            }

            EndUser newUser = CreateNewDefaultUser();
            newUser.UserName = networkCredentials.UserName;
            newUser.AppCredentials = Security.CreateAppCredentials(networkCredentials.SecurePassword);

            IEnumerable<ValidationResult> validationResults = newUser.Validate();
            if (validationResults.Any())
            {
                _context.SaveAndReload(_context.EndUsers.Remove(newUser));
                return validationResults;
            }

            _context.SaveAndReload(_context.Update(newUser));

            _sessionValueResolver.SetSessionUser(newUser);
            return [];
        }

        public EndUser? GetEndUser(int? id, params string[] relationships)
        {
            if (id == null)
            {
                return null;
            }

            return _context.GetEntityByKey<EndUser>(id.Value, relationships);
        }

        public IEnumerable<ValidationResult> SaveEndUser(EndUser user)
        {
            IEnumerable<ValidationResult> results = user.Validate();

            if (!results.Any())
            {
                _context.SaveAndReload(_context.EndUsers.Update(user));
            }
            
            return results;
        }
    }
}
