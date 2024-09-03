using CaptaTrader.Data.Base.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CaptaTrader.Data
{
    /// <summary>
    /// CRUD wrapper for a DbContext built with dynamic DbSet<> members.
    /// </summary>
    public class DynamicEmbeddedDataAccess : IDataAccess
    {
        private ILogger _logger;
        private DbContext _dbContext;

        public DynamicEmbeddedDataAccess(ILogger logger, DbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public void Create<T>(T entity)
        {
            if (entity != null)
            {
                EntityEntry dbEntry = _dbContext.Add(entity);

                SaveAndReload(dbEntry);
            }
        }

        public void CreateMany<T>(IEnumerable<T> entities)
        {
            List<EntityEntry> dbEntries = [];

            foreach (T entity in entities)
            {
                if (entity != null)
                {
                    dbEntries.Add(_dbContext.Add(entity));
                }
            }

            SaveAndReload(dbEntries);
        }

        public T? Read<T>(int id) where T : class
        {
            return _dbContext.Find<T>(id);
        }

        public IEnumerable<T> ReadWhere<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return _dbContext.Set<T>().Where(predicate).IncludeAllRecursively();
        }

        public IEnumerable<T> ReadAll<T>() where T : class
        {
            return _dbContext.Set<T>().IncludeAllRecursively();
        }

        public void Update<T>(T entity)
        {
            if (entity != null)
            {
                EntityEntry dbEntry = _dbContext.Update(entity);

                SaveAndReload(dbEntry);
            }
        }

        public void Delete<T>(T entity)
        {
            if (entity != null)
            {
                EntityEntry dbEntry = _dbContext.Remove(entity);

                SaveAndReload(dbEntry);
            }
        }

        private void SaveAndReload(EntityEntry dbEntry)
        {
            SaveAndReload([dbEntry]);
        }

        private void SaveAndReload(IEnumerable<EntityEntry> dbEntries)
        {
            try
            {
                _dbContext.SaveChanges();

                foreach (EntityEntry dbEntry in dbEntries)
                {
                    dbEntry.Reload();
                }
            }
            catch (DbUpdateException e)
            {
                _logger.LogError(e, "Database save failed.");
            }
        }
    }
}
