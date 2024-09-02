using CaptaTrader.Data.Base;
using Microsoft.EntityFrameworkCore;

namespace CaptaTrader.Data.Database
{
    public class EmbeddedDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            Configure(options);
        }

        internal static void Configure(DbContextOptionsBuilder options)
        {
            string appFolderPath = IO.GetAppDirectoryPath();
            string appName = new DirectoryInfo(appFolderPath).Name;
            string dbPath = Path.Join(appFolderPath, $"{appName}.db");

            options.UseSqlite($"Data Source={dbPath}");
        }
    }
}
