using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Domain.Models;
using System.Diagnostics;

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
        bool allowWrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        if (!allowWrite && !SqliteStatementClassifier.IsReadOnly(sql))
        {
            throw new InvalidOperationException("当前查询模式为只读。请切换到可写模式后再执行会修改数据库的 SQL。");
        }

        var accessMode = allowWrite
            ? SqliteConnectionAccessMode.ReadWrite
            : SqliteConnectionAccessMode.ReadOnly;
        await using var connection = await _connectionFactory.CreateAsync(filePath, accessMode, cancellationToken);
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

        var category = SqliteStatementClassifier.Classify(sql);
        var operationLabel = allowWrite
            ? category switch
            {
                SqlStatementCategory.Dml => "DML 写入",
                SqlStatementCategory.Ddl => "DDL 变更",
                SqlStatementCategory.Maintenance => "维护操作",
                SqlStatementCategory.Transaction => "事务控制",
                _ => "查询"
            }
            : "查询";

        var message = reader.FieldCount > 0
            ? $"查询返回了 {rows.Count} 行。"
            : $"[{operationLabel}] 影响了 {affectedRows} 行。";

        return new QueryExecutionResult(columns, rows, affectedRows, stopwatch.Elapsed, message);
    }
}
