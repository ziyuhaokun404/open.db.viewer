using System.Diagnostics;
using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Domain.Models;

namespace Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;

public sealed class SqliteQueryExecutor : ISqliteQueryExecutor
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteQueryExecutor(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<QueryExecutionResult> ExecuteAsync(
        string filePath,
        string sql,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        await using var connection = await _connectionFactory.CreateAsync(filePath, cancellationToken);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        var stopwatch = Stopwatch.StartNew();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var columns = new List<string>(reader.FieldCount);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            columns.Add(reader.GetName(i));
        }

        var rows = new List<IReadOnlyList<object?>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values);
        }

        stopwatch.Stop();

        var affectedRows = reader.FieldCount > 0 ? rows.Count : Math.Max(reader.RecordsAffected, 0);
        var message = reader.FieldCount > 0
            ? $"查询返回了 {rows.Count} 行。"
            : $"查询影响了 {affectedRows} 行。";

        return new QueryExecutionResult(columns, rows, affectedRows, stopwatch.Elapsed, message);
    }
}
