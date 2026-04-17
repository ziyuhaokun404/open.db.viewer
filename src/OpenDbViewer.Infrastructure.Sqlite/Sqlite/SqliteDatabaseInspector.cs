using System.Data.Common;
using OpenDbViewer.Application.Abstractions;
using OpenDbViewer.Domain.Models;

namespace OpenDbViewer.Infrastructure.Sqlite.Sqlite;

public sealed class SqliteDatabaseInspector
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteDatabaseInspector(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<string>> GetTablesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateAsync(filePath, cancellationToken);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT name
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%'
            ORDER BY name;
            """;

        var tables = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    public async Task<TableSchema> GetSchemaAsync(string filePath, string tableName, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateAsync(filePath, cancellationToken);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""PRAGMA table_info({QuoteIdentifier(tableName)});""";

        var columns = new List<TableColumnInfo>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var name = reader.GetString(reader.GetOrdinal("name"));
            var dataType = reader.GetString(reader.GetOrdinal("type"));
            var defaultValue = reader.IsDBNull(reader.GetOrdinal("dflt_value"))
                ? null
                : reader.GetValue(reader.GetOrdinal("dflt_value"))?.ToString();
            var isPrimaryKey = reader.GetInt32(reader.GetOrdinal("pk")) > 0;
            var isNullable = reader.GetInt32(reader.GetOrdinal("notnull")) == 0 && !isPrimaryKey;

            columns.Add(new TableColumnInfo(name, dataType, isNullable, defaultValue, isPrimaryKey));
        }

        return new TableSchema(tableName, columns);
    }

    private static string QuoteIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
