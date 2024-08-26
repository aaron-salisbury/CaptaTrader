using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CaptaTrader.Data.Base.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddDataAccess<T>(this IServiceCollection services) where T : DbContext
        {
            if (Activator.CreateInstance(typeof(T)) is T dbContext)
            {
                Directory.CreateDirectory(IO.GetAppDirectoryPath());
                dbContext.Database.EnsureCreated();

                services.AddSingleton(dbContext);
            }

            return services;
        }
    }
}
