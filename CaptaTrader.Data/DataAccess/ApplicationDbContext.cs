using CaptaTrader.Data.Base;
using Microsoft.EntityFrameworkCore;

namespace CaptaTrader.Data.DataAccess
{
    public class ApplicationDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            string appFolderPath = IO.GetAppDirectoryPath();
            string appName = new DirectoryInfo(appFolderPath).Name;
            string dbPath = Path.Join(appFolderPath, $"{appName}.db");

            options.UseSqlite($"Data Source={dbPath}");
        }
    }
}
