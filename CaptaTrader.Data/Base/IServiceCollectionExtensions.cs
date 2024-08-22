using CaptaTrader.Data.Database;
using Microsoft.Extensions.DependencyInjection;

namespace CaptaTrader.Data.Base
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabaseAccess(this IServiceCollection services)
        {
            ApplicationDbContext dbContext = new();

            dbContext.Database.EnsureCreated();

            services.AddSingleton(typeof(DbAccess), new DbAccess() { DbContext = dbContext });

            return services;
        }
    }
}
