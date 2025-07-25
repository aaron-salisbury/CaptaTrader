using CaptaTrader.Data.Base;
using CaptaTrader.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace CaptaTrader.Data.Database;

public sealed class ApplicationDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        string appFolderPath = IO.GetAppDirectoryPath();
        string appName = new DirectoryInfo(appFolderPath).Name;
        string dbPath = Path.Join(appFolderPath, $"{appName}.db");

        options.UseSqlite($"Data Source={dbPath}");
    }

    internal DbSet<EndUser> EndUsers { get; set; }
    internal DbSet<UserCredential> UserCredentials { get; set; }
    internal DbSet<UserConfig> UserConfigs { get; set; }
    internal DbSet<TradeOrder> TradeOrders { get; set; }
    internal DbSet<CryptoAsset> CryptoAssets { get; set; }
}
