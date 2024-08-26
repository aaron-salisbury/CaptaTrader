using CaptaTrader.Data.Base;
using CaptaTrader.Models;
using Microsoft.EntityFrameworkCore;

namespace CaptaTrader.Data.DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<AppCredentials> AppCredentials { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string appFolderPath = IO.GetAppDirectoryPath();
            string appName = new DirectoryInfo(appFolderPath).Name;
            string dbPath = Path.Join(appFolderPath, $"{appName}.db");

            options.UseSqlite($"Data Source={dbPath}");
        }
    }
}
