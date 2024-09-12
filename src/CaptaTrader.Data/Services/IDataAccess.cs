using System.Linq.Expressions;

namespace CaptaTrader.Data;

public interface IDataAccess
{
    TEntity Create<TEntity>(TEntity entity);
    void CreateRange<TEntity>(IEnumerable<TEntity> entities);
    TEntity? Read<TEntity>(int id) where TEntity : class;
    Task<TEntity?> ReadAsync<TEntity>(int id) where TEntity : class;
    IEnumerable<TEntity> ReadFiltered<TEntity>(Expression<Func<TEntity, bool>> predicate, params string[] relationships) where TEntity : class;
    IEnumerable<TEntity> ReadAll<TEntity>(params string[] relationships) where TEntity : class;
    int Count<TEntity>() where TEntity : class;
    Task<int> CountAsync<TEntity>() where TEntity : class;
    TEntity Update<TEntity>(TEntity entity);
    void Delete<TEntity>(TEntity entity);
    void Dispose();
}
