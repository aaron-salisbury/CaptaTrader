using CaptaTrader.Data.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CaptaTrader.Data
{
    public class DataAccesor
    {
        private ILogger _logger;
        private DbContext _dbContext;

        public DataAccesor(ILogger logger, DbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public void Create<T>(T entity) where T : BaseEntity
        {
            EntityEntry dbEntry = _dbContext.Add(entity);

            SaveAndReload(dbEntry);
        }

        public void CreateMany<T>(IEnumerable<T> entities) where T : BaseEntity
        {
            IEnumerable<EntityEntry> dbEntries = entities.Select(entity => _dbContext.Add(entity));

            SaveAndReload(dbEntries);
        }

        public T? Read<T>(int id) where T : BaseEntity
        {
            return _dbContext.Find<T>(id);
        }

        public IEnumerable<T> ReadWhere<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
        {
            return _dbContext.Set<T>().Where(predicate);
        }

        public IEnumerable<T> ReadAll<T>() where T : BaseEntity
        {
            return _dbContext.Set<T>().ToList();
        }

        public void Update<T>(T entity) where T : BaseEntity
        {
            EntityEntry dbEntry = _dbContext.Update(entity);

            SaveAndReload(dbEntry);
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
