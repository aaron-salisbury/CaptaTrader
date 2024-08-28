using CaptaTrader.Data.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CaptaTrader.Data.Base.Extensions
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a singleton embedded SQLite service of the type DbContext 
        /// and a singleton service of the type DataAccesor that is a wrapper for the DbContext.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="entityTypes">Must inherit from CaptaTrader.Data.Base.BaseEntity</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddDataAccessEmbedded(this IServiceCollection services, Type[] entityTypes)
        {
            Type dynamicDbContextType = BuildDynamicDbContextType<EmbeddedDbContext>(entityTypes);

            if (Activator.CreateInstance(dynamicDbContextType) is EmbeddedDbContext dbContext)
            {
                Directory.CreateDirectory(IO.GetAppDirectoryPath());
                dbContext.Database.EnsureCreated();

                services.AddSingleton<DbContext>(dbContext);
                services.AddSingleton<DataAccesor>();
            }

            return services;
        }

        private static Type BuildDynamicDbContextType<T>(Type[] entityTypes) where T : DbContext
        {
            List<DynamicProperty> properties = [];

            foreach (var entityType in entityTypes)
            {
                Type dbSetType = typeof(DbSet<>);
                Type dbSetGenericType = dbSetType.MakeGenericType(entityType);

                properties.Add(new DynamicProperty(entityType.Name, dbSetGenericType));
            }

            return DynamicClassFactory.CreateType(properties, typeof(T), false);
        }
    }
}
