using CaptaTrader.Data.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace CaptaTrader.Data
{
    public class DataAccesor
    {
        private DbContext _dbContext;

        public DataAccesor(DbContext dbContext)
        {
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
            SaveAndReload(new List<EntityEntry>() { dbEntry });
        }

        private void SaveAndReload(IEnumerable<EntityEntry> dbEntries)
        {
            _dbContext.SaveChanges();

            foreach (var dbEntry in dbEntries)
            {
                dbEntry.Reload();
            }
        }
    }
}
