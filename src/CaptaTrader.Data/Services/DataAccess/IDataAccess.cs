using System.Collections.Generic;
using System.Threading.Tasks;

namespace CaptaTrader.Data.Services;

/// <summary>
/// Defines a contract for generic asynchronous CRUD operations that execute against a SQL database.
/// </summary>
public interface ISQLDataAccess
{
    /// <summary>
    /// Asynchronously inserts the specified entity into the database table matching its type name.
    /// Only the properties present on the object (excluding the primary key) are used for the insert.
    /// The primary key value is not set back on the object.
    /// </summary>
    /// <typeparam name="T">The type of the entity to insert.</typeparam>
    /// <param name="entity">The entity to insert.</param>
    /// <returns>
    /// A <see cref="Task{long?}"/> representing the asynchronous operation.
    /// The task result contains the new primary key as a <see cref="long"/> if successful; otherwise, <c>null</c>.
    /// </returns>
    Task<long?> CreateAsync<T>(T entity);

    /// <summary>
    /// Asynchronously executes a raw SQL read query against the database and returns the results as a sequence of dynamic objects.
    /// </summary>
    /// <param name="sql">The raw SQL query to execute.</param>
    /// <param name="parameters">An optional object containing the parameters to use with the query.</param>
    /// <returns>
    /// A <see cref="Task{IEnumerable{dynamic}}"/> representing the asynchronous operation.
    /// The task result contains an <see cref="IEnumerable{dynamic}"/> with the results of the query.
    /// </returns>
    Task<IEnumerable<dynamic>> ReadRawAsync(string sql, object? parameters = null);

    /// <summary>
    /// Asynchronously reads all rows from the table corresponding to the specified type <typeparamref name="T"/>.
    /// Optionally filter results using a WHERE clause (without the 'WHERE' keyword) and optionally select specific columns.
    /// </summary>
    /// <typeparam name="T">The type whose table to read from. The table name must match the type name.</typeparam>
    /// <param name="whereClause">Optional SQL WHERE clause (without the 'WHERE' keyword) to filter results.</param>
    /// <param name="parameters">Optional parameters for the WHERE clause.</param>
    /// <param name="columns">Optional array of column names to retrieve.</param>
    /// <returns>
    /// A <see cref="Task{IEnumerable{T}}"/> representing the asynchronous operation.
    /// The task result contains an <see cref="IEnumerable{T}"/> representing all (or filtered) rows in the table.
    /// </returns>
    Task<IEnumerable<T>> ReadAsync<T>(string? whereClause = null, object? parameters = null, params string[]? columns);

    /// <summary>
    /// Asynchronously reads a single row from the table corresponding to the specified type <typeparamref name="T"/> by primary key,
    /// returning the specified columns or all columns if none are specified.
    /// </summary>
    /// <typeparam name="T">The type to which the result should be mapped. The table name must match the type name.</typeparam>
    /// <param name="key">The primary key value of the row to retrieve.</param>
    /// <param name="columns">An optional array of column names to retrieve. If null or empty, all columns are selected.</param>
    /// <returns>
    /// A <see cref="Task{T?}"/> representing the asynchronous operation.
    /// The task result contains the row mapped to <typeparamref name="T"/> if found; otherwise, <c>null</c>.
    /// </returns>
    Task<T?> ReadByPrimaryKeyAsync<T>(long key, params string[]? columns);

    /// <summary>
    /// Asynchronously updates a row in the table corresponding to the specified type <typeparamref name="T"/> by primary key,
    /// setting the provided column values.
    /// </summary>
    /// <typeparam name="T">The type whose table to update. The table name must match the type name.</typeparam>
    /// <param name="key">The primary key value of the row to update.</param>
    /// <param name="columnValues">A dictionary of column names and their new values.</param>
    /// <returns>
    /// A <see cref="Task{bool}"/> representing the asynchronous operation.
    /// The task result is <c>true</c> if a row was updated; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> UpdateByPrimaryKeyAsync<T>(long key, IDictionary<string, object?> columnValues);

    /// <summary>
    /// Asynchronously deletes a row from the table corresponding to the specified type <typeparamref name="T"/> by primary key.
    /// </summary>
    /// <typeparam name="T">The type whose table to delete from. The table name must match the type name.</typeparam>
    /// <param name="key">The primary key value of the row to delete.</param>
    /// <returns>
    /// A <see cref="Task{bool}"/> representing the asynchronous operation.
    /// The task result is <c>true</c> if a row was deleted; otherwise, <c>false</c>.
    /// </returns>
    Task<bool> DeleteByPrimaryKeyAsync<T>(long key);
}
