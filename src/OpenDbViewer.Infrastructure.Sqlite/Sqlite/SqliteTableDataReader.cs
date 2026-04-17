using Microsoft.Data.Sqlite;
using OpenDbViewer.Application.Abstractions;
using OpenDbViewer.Domain.Models;

namespace OpenDbViewer.Infrastructure.Sqlite.Sqlite;

public sealed class SqliteTableDataReader
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteTableDataReader(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<TablePageResult> ReadPageAsync(
        string filePath,
        string tableName,
        int pageNumber,
        int pageSize,
        string? sortColumn = null,
        string? sortDirection = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        await using var connection = await _connectionFactory.CreateAsync(filePath, cancellationToken);
        await connection.OpenAsync(cancellationToken);

        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = $"""PRAGMA table_info({QuoteIdentifier(tableName)});""";

        var columns = new List<string>();
        await using (var schemaReader = await schemaCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await schemaReader.ReadAsync(cancellationToken))
            {
                columns.Add(schemaReader.GetString(schemaReader.GetOrdinal("name")));
            }
        }

        if (columns.Count == 0)
        {
            return new TablePageResult(Array.Empty<string>(), Array.Empty<IReadOnlyList<object?>>(), pageNumber, pageSize, false, sortColumn, NormalizeSortDirection(sortDirection));
        }

        var orderByColumn = string.IsNullOrWhiteSpace(sortColumn) ? columns[0] : sortColumn;
        var normalizedSortDirection = NormalizeSortDirection(sortDirection);
        var offset = (pageNumber - 1) * pageSize;
        var limit = pageSize + 1;

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT {string.Join(", ", columns.Select(QuoteIdentifier))}
            FROM {QuoteIdentifier(tableName)}
            ORDER BY {QuoteIdentifier(orderByColumn)} {normalizedSortDirection}
            LIMIT $limit OFFSET $offset;
            """;
        command.Parameters.Add(new SqliteParameter("$limit", limit));
        command.Parameters.Add(new SqliteParameter("$offset", offset));

        var rows = new List<IReadOnlyList<object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new object[reader.FieldCount];
            reader.GetValues(row);
            rows.Add(row);
        }

        var hasNextPage = rows.Count > pageSize;
        if (hasNextPage)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return new TablePageResult(columns, rows, pageNumber, pageSize, hasNextPage, sortColumn, normalizedSortDirection);
    }

    private static string NormalizeSortDirection(string? sortDirection) =>
        string.Equals(sortDirection, "DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";

    private static string QuoteIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
