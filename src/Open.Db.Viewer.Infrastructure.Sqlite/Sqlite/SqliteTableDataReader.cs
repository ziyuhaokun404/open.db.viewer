using Microsoft.Data.Sqlite;
using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Domain.Sqlite;

namespace Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;

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
        IReadOnlyList<TableFilter>? filters = null,
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

        await using var connection = await _connectionFactory.CreateAsync(filePath, cancellationToken: cancellationToken);
        await connection.OpenAsync(cancellationToken);

        var columns = await ReadColumnNamesAsync(connection, tableName, cancellationToken);
        if (columns.Count == 0)
        {
            return new TablePageResult(
                Array.Empty<string>(),
                Array.Empty<IReadOnlyList<object?>>(),
                pageNumber,
                pageSize,
                false,
                sortColumn,
                NormalizeSortDirection(sortDirection),
                TotalRowCount: 0);
        }

        var validatedFilters = ValidateFilters(columns, filters);
        var orderByColumn = ResolveSortColumn(columns, sortColumn);
        var normalizedSortDirection = NormalizeSortDirection(sortDirection);
        var offset = (pageNumber - 1) * pageSize;
        var limit = pageSize + 1;

        var totalRowCount = await CountRowsAsync(connection, tableName, validatedFilters, cancellationToken);

        await using var command = connection.CreateCommand();
        var whereSql = AppendFilterClause(command, validatedFilters);
        command.CommandText = $"""
            SELECT {string.Join(", ", columns.Select(SqliteIdentifier.Quote))}
            FROM {SqliteIdentifier.Quote(tableName)}
            {whereSql}
            ORDER BY {SqliteIdentifier.Quote(orderByColumn)} {normalizedSortDirection}
            LIMIT $limit OFFSET $offset;
            """;
        command.Parameters.Add(new SqliteParameter("$limit", limit));
        command.Parameters.Add(new SqliteParameter("$offset", offset));

        var rows = new List<IReadOnlyList<object?>>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        var hasNextPage = rows.Count > pageSize;
        if (hasNextPage)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        return new TablePageResult(
            columns,
            rows,
            pageNumber,
            pageSize,
            hasNextPage,
            orderByColumn,
            normalizedSortDirection,
            totalRowCount);
    }

    public async IAsyncEnumerable<IReadOnlyList<object?>> StreamRowsAsync(
        string filePath,
        string tableName,
        string? sortColumn = null,
        string? sortDirection = null,
        IReadOnlyList<TableFilter>? filters = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateAsync(filePath, cancellationToken: cancellationToken);
        await connection.OpenAsync(cancellationToken);

        var columns = await ReadColumnNamesAsync(connection, tableName, cancellationToken);
        if (columns.Count == 0)
        {
            yield break;
        }

        var validatedFilters = ValidateFilters(columns, filters);
        var orderByColumn = ResolveSortColumn(columns, sortColumn);
        var normalizedSortDirection = NormalizeSortDirection(sortDirection);

        await using var command = connection.CreateCommand();
        var whereSql = AppendFilterClause(command, validatedFilters);
        command.CommandText = $"""
            SELECT {string.Join(", ", columns.Select(SqliteIdentifier.Quote))}
            FROM {SqliteIdentifier.Quote(tableName)}
            {whereSql}
            ORDER BY {SqliteIdentifier.Quote(orderByColumn)} {normalizedSortDirection};
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new object?[reader.FieldCount];
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            yield return row;
        }
    }

    private static async Task<IReadOnlyList<string>> ReadColumnNamesAsync(
        System.Data.Common.DbConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = $"""PRAGMA table_info({SqliteIdentifier.Quote(tableName)});""";

        var columns = new List<string>();
        await using var schemaReader = await schemaCommand.ExecuteReaderAsync(cancellationToken);
        while (await schemaReader.ReadAsync(cancellationToken))
        {
            columns.Add(schemaReader.GetString(schemaReader.GetOrdinal("name")));
        }

        return columns;
    }

    private static async Task<long> CountRowsAsync(
        System.Data.Common.DbConnection connection,
        string tableName,
        IReadOnlyList<TableFilter> filters,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        var whereSql = AppendFilterClause(command, filters);
        command.CommandText = $"""
            SELECT COUNT(*)
            FROM {SqliteIdentifier.Quote(tableName)}
            {whereSql};
            """;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<TableFilter> ValidateFilters(
        IReadOnlyList<string> columns,
        IReadOnlyList<TableFilter>? filters)
    {
        if (filters is null || filters.Count == 0)
        {
            return Array.Empty<TableFilter>();
        }

        var columnSet = new HashSet<string>(columns, StringComparer.OrdinalIgnoreCase);
        var validated = new List<TableFilter>(filters.Count);
        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter.Column) || !columnSet.Contains(filter.Column))
            {
                throw new ArgumentException($"筛选列不存在：{filter.Column}", nameof(filters));
            }

            var actualColumn = columns.First(c => c.Equals(filter.Column, StringComparison.OrdinalIgnoreCase));
            validated.Add(filter with { Column = actualColumn });
        }

        return validated;
    }

    private static string ResolveSortColumn(IReadOnlyList<string> columns, string? sortColumn)
    {
        if (string.IsNullOrWhiteSpace(sortColumn))
        {
            return columns[0];
        }

        var match = columns.FirstOrDefault(c => c.Equals(sortColumn, StringComparison.OrdinalIgnoreCase));
        return match ?? columns[0];
    }

    /// <summary>
    /// 将筛选条件追加为参数化 WHERE 子句；返回 "WHERE ..." 或空字符串。
    /// </summary>
    private static string AppendFilterClause(System.Data.Common.DbCommand command, IReadOnlyList<TableFilter> filters)
    {
        if (filters.Count == 0)
        {
            return string.Empty;
        }

        var parts = new List<string>(filters.Count);
        for (var i = 0; i < filters.Count; i++)
        {
            var filter = filters[i];
            var columnSql = SqliteIdentifier.Quote(filter.Column);
            var paramName = $"$f{i}";

            switch (filter.Operator)
            {
                case TableFilterOperator.IsNull:
                    parts.Add($"{columnSql} IS NULL");
                    break;
                case TableFilterOperator.IsNotNull:
                    parts.Add($"{columnSql} IS NOT NULL");
                    break;
                case TableFilterOperator.Equals:
                    parts.Add($"CAST({columnSql} AS TEXT) = {paramName}");
                    AddParameter(command, paramName, filter.Value ?? string.Empty);
                    break;
                case TableFilterOperator.Contains:
                default:
                    parts.Add($"CAST({columnSql} AS TEXT) LIKE {paramName} ESCAPE '\\'");
                    AddParameter(command, paramName, BuildContainsPattern(filter.Value));
                    break;
            }
        }

        return "WHERE " + string.Join(" AND ", parts);
    }

    private static string BuildContainsPattern(string? value)
    {
        var raw = value ?? string.Empty;
        var escaped = raw
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        return $"%{escaped}%";
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string NormalizeSortDirection(string? sortDirection) =>
        string.Equals(sortDirection, "DESC", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
}
