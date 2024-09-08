using CaptaTrader.Data.Base;
using CaptaTrader.Data.Base.Extensions;
using CaptaTrader.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;

namespace CaptaTrader.Data.Database
{
    //TODO: Read and apply: https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries

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
            try
            {
                Save();

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

        //TODO: Test this.
        public EndUser? GetEndUser(int id, params string[] relationships)
        {
            if (relationships.Length != 0)
            {
                return EndUsers.Where(eu => eu.Id == id)
                    .IncludeRelationshipsRecursively(new Stack<string>(relationships))
                    .SingleOrDefault();
            }

            return EndUsers.Find(id);
        }
    }
}
