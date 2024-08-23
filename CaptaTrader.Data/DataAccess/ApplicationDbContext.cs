using CaptaTrader.Data.Base;
using CaptaTrader.Models;
using Microsoft.EntityFrameworkCore;

namespace CaptaTrader.Data.DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        public string DbPath { get; } //TODO: Connection strings should not be stored in the code for production applications.

        public ApplicationDbContext()
        {
            string appFolderPath = IO.GetAppDirectoryPath();
            string appName = new DirectoryInfo(appFolderPath).Name;

            DbPath = Path.Join(appFolderPath, $"{appName}.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");
    }
}
