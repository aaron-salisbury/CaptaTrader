using CaptaTrader.Models;
using Microsoft.EntityFrameworkCore;

namespace CaptaTrader.Data.Database
{
    public class AppCredentialsDbContext : DbContext
    {
        public DbSet<AppCredentials> AppCredentials { get; set; }

        public string DbPath { get; } //TODO: Connection strings should not be stored in the code for production applications.

        public AppCredentialsDbContext()
        {
            string appFolderPath = CRUD.GetAppDirectoryPath();
            string dbName = $"{nameof(AppCredentials)}.db";

            DbPath = Path.Join(appFolderPath, dbName);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }
}
