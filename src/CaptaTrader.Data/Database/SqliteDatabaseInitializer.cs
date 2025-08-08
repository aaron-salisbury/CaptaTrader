using CaptaTrader.Data.Database.Migrations;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using ROSIO = RunnethOverStudio.AppToolkit.Modules.DataAccess.IO;

namespace CaptaTrader.Data.Database;

/// <summary>
/// Provides methods to ensure the application's database is created and up-to-date.
/// </summary>
public static class SqliteDatabaseInitializer
{
    private static readonly object _initLock = new();

    /// <summary>
    /// Gets the full file path to the application's SQLite database file.
    /// </summary>
    /// <returns>
    /// The absolute path to the database file, constructed from the application's directory and name.
    /// </returns>
    public static string GetDBPath()
    {
        string appFolderPath = ROSIO.GetAppDirectoryPath();
        string appName = new DirectoryInfo(appFolderPath).Name;

        return Path.Join(appFolderPath, $"{appName}.db");
    }

    /// <summary>
    /// Ensures the application's database exists and is current by creating it if necessary
    /// and applying any pending migrations.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe and should be called during application initialization 
    /// before configuring data access services.
    /// </remarks>
    public static void InitializeDatabase()
    {
        //SQLitePCL.Batteries.Init();

        lock (_initLock)
        {
            string dbPath = GetDBPath();

            try
            {
                if (File.Exists(dbPath))
                {
                    UpdateExistingSqliteDB(dbPath);
                }
                else
                {
                    CreateNewSqliteDB(dbPath);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize the database.", ex);
            }
        }
    }

    private static void CreateNewSqliteDB(string dbPath)
    {
        using SqliteConnection connection = new($"Data Source={dbPath}");
        connection.Open();

        // Create Migration table
        const string createMigrationTable = @"
            CREATE TABLE IF NOT EXISTS Migration (
                MigrationId INTEGER PRIMARY KEY AUTOINCREMENT,
                Number INTEGER NOT NULL,
                AppliedOn TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP)
            );
        ";

        connection.Execute(createMigrationTable);

        RunMigrations(connection, 1);
    }

    private static void UpdateExistingSqliteDB(string dbPath)
    {
        using SqliteConnection connection = new($"Data Source={dbPath}");
        connection.Open();

        const string sql = "SELECT MAX(Number) FROM Migration;";
        uint lastMigrationNumber = connection.ExecuteScalar<uint>(sql);

        RunMigrations(connection, ++lastMigrationNumber);
    }

    private static void RunMigrations(DbConnection connection, uint startingNumber)
    {
        List<BaseMigration> migrations = [];

        foreach (Type assemblyType in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (!assemblyType.IsAbstract && typeof(BaseMigration).IsAssignableFrom(assemblyType))
            {
                string name = assemblyType.Name;
                int underscoreIndex = name.LastIndexOf('_');
                if (underscoreIndex >= 0 && underscoreIndex < name.Length - 1)
                {
                    string numberPart = name[(underscoreIndex + 1)..];

                    if (uint.TryParse(numberPart, out uint migrationNumber) && migrationNumber >= startingNumber)
                    {
                        migrations.Add((BaseMigration)Activator.CreateInstance(assemblyType)!);
                    }
                }
            }
        }

        foreach (BaseMigration migration in migrations.OrderBy(m => m.Number()))
        {
            migration.Run(connection);
        }
    }
}
