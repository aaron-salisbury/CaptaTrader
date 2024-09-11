using CaptaTrader.Data.Base;
using CaptaTrader.Data.Base.Extensions;
using CaptaTrader.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;

namespace CaptaTrader.Data.Database;

//TODO: Read and apply:
//  https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries
//  https://learn.microsoft.com/en-us/ef/core/modeling/indexes?tabs=data-annotations

public class ApplicationDbContext : DbContext
{
    private readonly ILogger _logger;

    public ApplicationDbContext(ILogger logger)
    {
        _logger = logger;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        string appFolderPath = IO.GetAppDirectoryPath();
        string appName = new DirectoryInfo(appFolderPath).Name;
        string dbPath = Path.Join(appFolderPath, $"{appName}.db");

        //TODO: Don't hard-code connection strings; should be stored in configuration.
        options.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExecutedTrade>().OwnsOne(et => et.TradeOrder, builder => { builder.ToJson(); });

        base.OnModelCreating(modelBuilder);
    }

    public DbSet<EndUser> EndUsers { get; set; }
    public DbSet<AppCredentials> AppCredentials { get; set; }
    public DbSet<ExecutedTrade> ExecutedTrades { get; set; }

    public void SaveAndReload(EntityEntry dbEntry)
    {
        SaveAndReload([dbEntry]);
    }

    public void SaveAndReload(IEnumerable<EntityEntry> dbEntries)
    {
        Save();

        foreach (EntityEntry dbEntry in dbEntries)
        {
            dbEntry.Reload();
        }
    }

    public void Save()
    {
        try
        {
            SaveChanges();
        }
        catch (DbUpdateException e)
        {
            _logger.LogError(e, "Database save failed.");
        }
    }

    public TEntity? GetEntityByKey<TEntity>(int id, params string[] relationships) where TEntity : class, IEntity
    {
        if (relationships.Length != 0)
        {
            return Set<TEntity>()
                .Where(e => e.Id == id)
                .IncludeRelationships(new Stack<string>(relationships))
                .SingleOrDefault();
        }

        return Find<TEntity>(id);
    }
}
