using CaptaTrader.Data.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CaptaTrader.Data.Base.Extensions
{
    public static class IServiceCollectionExtensions
    {
        private static Type? _dynamicDbContextType;

        /// <summary>
        /// Adds a scoped DbContext service for an embedded SQLite database built against the provided entity types. 
        /// Also adds a scoped service of the type DataAccesor that is a wrapper for the DbContext.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
        /// <param name="entityTypes">Must inherit from CaptaTrader.Data.Base.BaseEntity</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddDataAccessEmbedded(this IServiceCollection services, Type[] entityTypes)
        {
            _dynamicDbContextType = BuildDynamicDbContextType<EmbeddedDbContext>(entityTypes);

            services.AddScoped(typeof(DbContext), DynamicContextFactory);
            services.AddScoped<DataAccesor>();

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

        private static DbContext DynamicContextFactory(IServiceProvider provider)
        {
            if (_dynamicDbContextType == null) { throw new NullReferenceException("_dynamicDbContextType must be set prior to using this factory to scope that type."); }

            if (Activator.CreateInstance(_dynamicDbContextType) is not DbContext dbContext) { throw new Exception("Not able to create an instance of the dynamic DbContext."); }

            dbContext.Database.EnsureCreated();

            return dbContext;
        }
    }
}
