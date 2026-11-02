using Microsoft.Data.Sqlite;
using System.Threading;
using System.Threading.Tasks;

namespace CaptaTrader.Data.Database;

internal interface IApplicationDatabase
{
    Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
