using CaptaTrader.Data.Base.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace CaptaTrader.Data;

/// <summary>
/// CRUD wrapper for DbContext.
/// </summary>
public class DataAccess : IDataAccess, IDisposable
{
    private readonly ILogger _logger;
    private readonly DbContext _context;
    private bool _disposed;

    public DataAccess(ILogger logger, DbContext dbContext)
    {
        _logger = logger;
        _context = dbContext;
    }

    public TEntity Create<TEntity>(TEntity entity)
    {
        if (entity != null)
        {
            EntityEntry dbEntry = _context.Add(entity);

            SaveAndReload(dbEntry);
        }

        return entity;
    }

    public void CreateRange<TEntity>(IEnumerable<TEntity> entities)
    {
        List<EntityEntry> dbEntries = [];

        foreach (TEntity entity in entities)
        {
            if (entity != null)
            {
                dbEntries.Add(_context.Add(entity));
            }
        }

        SaveAndReload(dbEntries);
    }

    public TEntity? Read<TEntity>(int id) where TEntity : class
    {
        return _context.Set<TEntity>().Find(id);
    }

    public async Task<TEntity?> ReadAsync<TEntity>(int id) where TEntity : class
    {
        return await _context.Set<TEntity>().FindAsync(id);
    }

    public IEnumerable<TEntity> ReadFiltered<TEntity>(Expression<Func<TEntity, bool>> predicate, params string[] relationships) where TEntity : class
    {
        IQueryable<TEntity > query = _context.Set<TEntity>().Where(predicate);

        if (relationships.Length != 0)
        {
            query = query.IncludeRelationships(new Stack<string>(relationships));
        }

        return query.AsEnumerable();
    }

    public IEnumerable<TEntity> ReadAll<TEntity>(params string[] relationships) where TEntity : class
    {
        IQueryable<TEntity> query = _context.Set<TEntity>();

        if (relationships.Length != 0)
        {
            query = query.IncludeRelationships(new Stack<string>(relationships));
        }

        return query.AsEnumerable();
    }

    public TEntity Update<TEntity>(TEntity entity)
    {
        if (entity != null)
        {
            EntityEntry dbEntry = _context.Update(entity);

            SaveAndReload(dbEntry);
        }

        return entity;
    }

    public void Delete<TEntity>(TEntity entity)
    {
        if (entity != null)
        {
            EntityEntry dbEntry = _context.Remove(entity);

            SaveAndReload(dbEntry);
        }
    }

    public int Count<TEntity>() where TEntity : class
    {
        return _context.Set<TEntity>().Count();
    }

    public async Task<int> CountAsync<TEntity>() where TEntity : class
    {
        return await _context.Set<TEntity>().CountAsync();
    }

    private void SaveAndReload(EntityEntry dbEntry)
    {
        SaveAndReload([dbEntry]);
    }

    private void SaveAndReload(IEnumerable<EntityEntry> dbEntries)
    {
        try
        {
            _context.SaveChanges();

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

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
