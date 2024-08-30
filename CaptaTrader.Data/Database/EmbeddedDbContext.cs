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
            //TODO: It would be nice to let the user change where the DB gets stored.
            // For instance, if they'd like to place it in a share folder where separate
            // instances of the application can all point to it. Could use 
            // InternalStorage.UserStorageDirectory to keep track of the path.
            // https://code-maze.com/efcore-dynamically-switching-dbcontext-at-runtime/

            string appFolderPath = IO.GetAppDirectoryPath();
            string appName = new DirectoryInfo(appFolderPath).Name;
            string dbPath = Path.Join(appFolderPath, $"{appName}.db");

            options.UseSqlite($"Data Source={dbPath}");
        }
    }
}
