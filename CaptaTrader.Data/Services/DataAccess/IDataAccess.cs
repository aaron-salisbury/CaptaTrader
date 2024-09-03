using System.Linq.Expressions;

namespace CaptaTrader.Data
{
    public interface IDataAccess
    {
        void Create<T>(T entity);
        void CreateMany<T>(IEnumerable<T> entities);
        void Delete<T>(T entity);
        T? Read<T>(int id) where T : class;
        IEnumerable<T> ReadAll<T>() where T : class;
        IEnumerable<T> ReadWhere<T>(Expression<Func<T, bool>> predicate) where T : class;
        void Update<T>(T entity);
    }
}
