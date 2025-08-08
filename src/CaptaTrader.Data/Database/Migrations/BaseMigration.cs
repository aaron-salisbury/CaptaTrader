using Dapper;
using System.Data.Common;

namespace CaptaTrader.Data.Database.Migrations;

internal abstract class BaseMigration
{
    public abstract uint Number();

    public void Run(DbConnection dbConnection)
    {
        Execute(dbConnection);
        UpdateMigrationTable(dbConnection);
    }

    protected abstract void Execute(DbConnection dbConnection);

    private void UpdateMigrationTable(DbConnection dbConnection)
    {
        const string sql = @"
            INSERT INTO Migration (Number)
            VALUES (@Number);
        ";

        dbConnection.Execute(sql, param: new
        {
            Number = this.Number()
        });
    }
}
