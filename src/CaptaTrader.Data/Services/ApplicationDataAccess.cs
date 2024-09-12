using CaptaTrader.Data.Database;
using Microsoft.Extensions.Logging;

namespace CaptaTrader.Data;

/// <summary>
/// CRUD wrapper for ApplicationDbContext.
/// </summary>
public class ApplicationDataAccess : DataAccess
{
    public ApplicationDataAccess(ILogger logger, ApplicationDbContext dbContext) : base(logger, dbContext) { }
}
