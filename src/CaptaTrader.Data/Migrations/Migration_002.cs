using Dapper;
using RunnethOverStudio.AppToolkit.Modules.DataAccess;
using System.Data.Common;

namespace CaptaTrader.Data.Migrations;

internal sealed class Migration_002 : BaseMigration
{
    public override uint Number() => 2;

    protected override void Execute(DbConnection dbConnection)
    {
        // Create EndUserConfig table
        const string createEndUserConfigTable = @"
            CREATE TABLE IF NOT EXISTS EndUserConfig (
                EndUserConfigId INTEGER PRIMARY KEY AUTOINCREMENT,
                EndUserId INTEGER NOT NULL,
                Email TEXT,
                MarketDataAPI INTEGER,
                MarketDataAPIKey TEXT,
                ExchangeAPI INTEGER,
                ExchangeAPIKey TEXT,
                ExchangeSecretKey TEXT,
                FOREIGN KEY (EndUserId) REFERENCES EndUser(EndUserId) ON DELETE CASCADE
            );
        ";

        // Create TradeOrder table
        const string createTradeOrderTable = @"
            CREATE TABLE IF NOT EXISTS TradeOrder (
                TradeOrderId INTEGER PRIMARY KEY AUTOINCREMENT,
                EndUserId INTEGER NOT NULL,
                TimeStamp TEXT NOT NULL,
                TransactionType INTEGER NOT NULL,
                AssetSymbolBuying TEXT NOT NULL,
                AssetSymbolSpending TEXT NOT NULL,
                Price DECIMAL NOT NULL,
                Amount DECIMAL NOT NULL,
                FOREIGN KEY (EndUserId) REFERENCES EndUser(EndUserId) ON DELETE CASCADE
            );
        ";

        dbConnection.Execute(createEndUserConfigTable);
        dbConnection.Execute(createTradeOrderTable);
    }
}
