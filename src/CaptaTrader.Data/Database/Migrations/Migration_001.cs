using Dapper;
using System.Data.Common;

namespace CaptaTrader.Data.Database.Migrations;

internal sealed class Migration_001 : BaseMigration
{
    public override uint Number() => 1;

    protected override void Execute(DbConnection dbConnection)
    {
        // Create EndUser table
        const string createEndUserTable = @"
            CREATE TABLE IF NOT EXISTS EndUser (
                EndUserId INTEGER PRIMARY KEY AUTOINCREMENT,
                Username TEXT NOT NULL UNIQUE,
                CreatedOn TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP)
            );
        ";

        // Create EndUserCredential table
        const string createEndUserCredentialTable = @"
            CREATE TABLE IF NOT EXISTS EndUserCredential (
                EndUserCredentialId INTEGER PRIMARY KEY AUTOINCREMENT,
                EndUserId INTEGER NOT NULL UNIQUE,
                LoginSalt BLOB NOT NULL,
                LoginHash BLOB NOT NULL,
                LoginWorkFactor INTEGER NOT NULL,
                FOREIGN KEY (EndUserId) REFERENCES EndUser(EndUserId) ON DELETE CASCADE
            );
        ";

        dbConnection.Execute(createEndUserTable);
        dbConnection.Execute(createEndUserCredentialTable);
    }
}
