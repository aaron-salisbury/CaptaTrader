using CaptaTrader.Data.Database;
using Microsoft.Extensions.DependencyInjection;

namespace CaptaTrader.Data.Base.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddCredentialsAccess(this IServiceCollection services)
        {
            AppCredentialsDbContext dbContext = new();

            dbContext.Database.EnsureCreated();

            services.AddSingleton(typeof(AppCredentialsDbContext), dbContext);

            return services;
        }

        public static IServiceCollection AddDataAccess(this IServiceCollection services)
        {
            ApplicationDbContext dbContext = new();

            dbContext.Database.EnsureCreated();

            services.AddSingleton(typeof(ApplicationDbContext), dbContext);

            return services;
        }
    }
}
