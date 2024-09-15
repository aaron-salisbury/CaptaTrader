using CaptaTrader.Data.Base;
using CaptaTrader.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CaptaTrader.Data.Database;

//TODO: Read and apply:
//  https://learn.microsoft.com/en-us/ef/core/querying/single-split-queries
//  https://learn.microsoft.com/en-us/ef/core/modeling/indexes?tabs=data-annotations
// https://learn.microsoft.com/en-us/ef/ef6/querying/related-data

public class ApplicationDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        string appFolderPath = IO.GetAppDirectoryPath();
        string appName = new DirectoryInfo(appFolderPath).Name;
        string dbPath = Path.Join(appFolderPath, $"{appName}.db");

        //TODO: Don't hard-code connection strings; should be stored in configuration.
        options.UseSqlite($"Data Source={dbPath}");
    }

    internal DbSet<EndUser> EndUsers { get; set; }
    internal DbSet<UserCredential> UserCredentials { get; set; }
    internal DbSet<UserConfig> UserConfigs { get; set; }
    internal DbSet<TradeOrder> TradeOrders { get; set; }
}
