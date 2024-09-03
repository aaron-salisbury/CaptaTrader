using CaptaTrader.Data.Base;
using CaptaTrader.Data.Database;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace CaptaTrader.Data
{
    public static class Startup
    {
        private static Type? _dynamicDbContextType;

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a named <see cref="HttpClient"/> used for compression.
        /// Also adds a singleton service of the type <see cref="WebRequester"/> that is a wrapper for web requests.
        /// </summary>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddWebRequesting(this IServiceCollection services)
        {
            services.AddHttpClient(WebRequester.COMPRESSION_CLIENT_NAME, c => { c.DefaultRequestHeaders.Add("Accept-Encoding", "deflate, gzip"); })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip
                });

            services.AddSingleton<WebRequester, WebRequester>();

            return services;
        }

        /// <summary>
        /// Adds a scoped DbContext service for an embedded SQLite database whose tables are built against the provided entity types. 
        /// No migrations are run.
        /// Also adds a scoped service of the type <see cref="IDataAccess"/> that is a wrapper for the DbContext.
        /// </summary>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddDataAccessEmbedded(this IServiceCollection services, Type[] entityTypes)
        {
            _dynamicDbContextType = BuildDynamicDbContextType<EmbeddedDbContext>(entityTypes);

            services.AddScoped(typeof(DbContext), DynamicContextFactory);
            services.AddScoped<IDataAccess, DynamicEmbeddedDataAccess>();

            return services;
        }

        private static Type BuildDynamicDbContextType<T>(Type[] entityTypes) where T : DbContext
        {
            List<DynamicProperty> properties = [];

            foreach (var entityType in entityTypes)
            {
                Type dbSetType = typeof(DbSet<>);
                Type dbSetGenericType = dbSetType.MakeGenericType(entityType);

                properties.Add(new DynamicProperty(entityType.Name.Pluralize(), dbSetGenericType));
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
