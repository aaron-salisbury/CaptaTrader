using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace CaptaTrader.Data.Services;

/// <summary>
/// Generic asynchronous CRUD operations that execute against a SQL database.
/// </summary>
/// <remarks>
/// This implementation uses Dapper for access to the app's SQLite database.
/// </remarks>
public class DapperSQLiteDataAccess : ISQLDataAccess
{
    private readonly string _dbPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="DapperSQLiteDataAccess"/> class using the specified SQLite database file path.
    /// </summary>
    /// <param name="appDBPath">The file path to the application's SQLite database.</param>
    public DapperSQLiteDataAccess(string appDBPath)
    {
        _dbPath = appDBPath;
    }

    /// <inheritdoc/>
    public async Task<long?> CreateAsync<T>(T entity)
    {
        Type type = typeof(T);
        string tableName = type.Name;
        List<PropertyInfo> properties = [.. type.GetProperties()
            .Where(p => !p.Name.Equals($"{tableName}Id", StringComparison.OrdinalIgnoreCase))];

        string columns = string.Join(", ", properties.Select(p => p.Name));
        string values = string.Join(", ", properties.Select(p => "@" + p.Name));
        string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values}); SELECT last_insert_rowid();";

        // Note: last_insert_rowid() works because we define our table ID columns with INTEGER PRIMARY KEY AUTOINCREMENT which then makes that column an alias for the internal ROWID.

        await using SqliteConnection conn = CreateConnection();
        await conn.OpenAsync();

        return await conn.ExecuteScalarAsync<long?>(sql, entity);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<dynamic>> ReadRawAsync(string sql, object? parameters = null)
    {
        await using SqliteConnection conn = CreateConnection();
        await conn.OpenAsync();

        return await conn.QueryAsync(sql, parameters);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> ReadAsync<T>(string? whereClause = null, object? parameters = null, params string[]? columns)
    {
        string tableName = typeof(T).Name;
        string selectColumns = (columns != null && columns.Length > 0) ? string.Join(", ", columns) : "*";
        string sql = $"SELECT {selectColumns} FROM {tableName}";
        if (!string.IsNullOrWhiteSpace(whereClause))
        {
            sql += $" WHERE {whereClause}";
        }

        await using SqliteConnection conn = CreateConnection();
        await conn.OpenAsync();
        return await conn.QueryAsync<T>(sql, parameters);
    }

    /// <inheritdoc/>
    public async Task<T?> ReadByPrimaryKeyAsync<T>(long key, params string[]? columns)
    {
        string tableName = typeof(T).Name;
        string idColumn = $"{tableName}Id";
        string selectColumns = (columns != null && columns.Length > 0) ? string.Join(", ", columns) : "*";
        string sql = $"SELECT {selectColumns} FROM {tableName} WHERE {idColumn} = @id LIMIT 1;";

        await using SqliteConnection conn = CreateConnection();
        await conn.OpenAsync();

        return await conn.QuerySingleOrDefaultAsync<T>(sql, new { id = key });
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateByPrimaryKeyAsync<T>(long key, IDictionary<string, object?> columnValues)
    {
        if (columnValues.Count == 0)
        {
            throw new ArgumentException("At least one column value must be provided.", nameof(columnValues));
        }

        string tableName = typeof(T).Name;
        string idColumn = $"{tableName}Id";
        string setClause = string.Join(", ", columnValues.Keys.Select(col => $"{col} = @{col}"));
        string sql = $"UPDATE {tableName} SET {setClause} WHERE {idColumn} = @id;";

        DynamicParameters parameters = new();
        parameters.Add("id", key);
        foreach (var kvp in columnValues)
        {
            parameters.Add(kvp.Key, kvp.Value);
        }

        await using SqliteConnection conn = CreateConnection();
        await conn.OpenAsync();

        int affected = await conn.ExecuteAsync(sql, parameters);
        return affected > 0;
    }

    /// <summary>
    /// Deletes a row from the table corresponding to the specified type <typeparamref name="T"/> by primary key.
    /// </summary>
    /// <typeparam name="T">The type whose table to delete from. The table name must match the type name.</typeparam>
    /// <param name="key">The primary key value of the row to delete.</param>
    /// <returns>True if a row was deleted; otherwise, false.</returns>
    public async Task<bool> DeleteByPrimaryKeyAsync<T>(long key)
    {
        string tableName = typeof(T).Name;
        string idColumn = $"{tableName}Id";
        string sql = $"DELETE FROM {tableName} WHERE {idColumn} = @id;";

        await using SqliteConnection conn = CreateConnection();
        await conn.OpenAsync();

        int affected = await conn.ExecuteAsync(sql, new { id = key });
        return affected > 0;
    }

    private SqliteConnection CreateConnection() => new($"Data Source={_dbPath}");
}
