using CaptaTrader.Business.Modules.UserAccess;
using CaptaTrader.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace CaptaTrader.Tests.Data;

[TestClass]
public sealed class ApplicationDatabaseInitializerTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task LegacyDatabase_IsMovedAsideUnchanged_AndANewOneCreated()
    {
        using TestDirectory directory = new();
        string databasePath = Path.Combine(directory.Path, DependencyInjection.DATABASE_FILE_NAME);

        await using (SqliteConnection legacy = new($"Data Source={databasePath};Pooling=False"))
        {
            await legacy.OpenAsync(TestContext.CancellationToken);
            await legacy.ExecuteAsync("CREATE TABLE EndUser (EndUserId INTEGER PRIMARY KEY, Username TEXT); INSERT INTO EndUser (Username) VALUES ('old');");
        }

        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);

        string backupPath = Assert.ContainsSingle(Directory.GetFiles(directory.Path, "*.legacy-*.db"));
        await using (SqliteConnection backup = new($"Data Source={backupPath};Mode=ReadOnly;Pooling=False"))
        {
            await backup.OpenAsync(TestContext.CancellationToken);
            Assert.AreEqual("old", await backup.ExecuteScalarAsync<string>("SELECT Username FROM EndUser;"));
        }

        Assert.IsNull(await services.GetRequiredService<IUserAccountStore>().GetAsync(TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task CurrentDatabase_IsNotMovedAside()
    {
        using TestDirectory directory = new();

        await using (await TestServices.CreateAsync(directory.Path))
        {
        }

        await using ServiceProvider services = await TestServices.CreateAsync(directory.Path);

        Assert.IsEmpty(Directory.GetFiles(directory.Path, "*.legacy-*.db"));
    }
}
