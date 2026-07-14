using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Domain.Sqlite;
using System.Data.Common;

namespace Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;

public sealed class SqliteDatabaseInspector
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public SqliteDatabaseInspector(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<string>> GetTablesAsync(string filePath, CancellationToken cancellationToken = default)
    {
        var catalog = await GetObjectCatalogAsync(filePath, includeSystemObjects: false, cancellationToken);
        return catalog
            .Where(node => node.Kind.Equals(DatabaseObjectKinds.Table, StringComparison.OrdinalIgnoreCase))
            .Select(node => node.Name)
            .ToArray();
    }

    /// <summary>
    /// 返回扁平对象列表（表/视图/索引/触发器，可选系统表）。
    /// </summary>
    public async Task<IReadOnlyList<DatabaseObjectNode>> GetObjectCatalogAsync(
        string filePath,
        bool includeSystemObjects = false,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateAsync(filePath, cancellationToken: cancellationToken);
        await connection.OpenAsync(cancellationToken);

        var nodes = new List<DatabaseObjectNode>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT type, name, tbl_name, sql
                FROM sqlite_master
                WHERE type IN ('table', 'view', 'index', 'trigger')
                ORDER BY
                    CASE type
                        WHEN 'table' THEN 0
                        WHEN 'view' THEN 1
                        WHEN 'index' THEN 2
                        WHEN 'trigger' THEN 3
                        ELSE 4
                    END,
                    name;
                """;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var type = reader.GetString(0);
                var name = reader.GetString(1);
                var tableName = reader.IsDBNull(2) ? null : reader.GetString(2);
                var sql = reader.IsDBNull(3) ? null : reader.GetString(3);

                if (type.Equals("table", StringComparison.OrdinalIgnoreCase) &&
                    name.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase))
                {
                    if (!includeSystemObjects)
                    {
                        continue;
                    }

                    nodes.Add(new DatabaseObjectNode(
                        Id: $"system:{name}",
                        Kind: DatabaseObjectKinds.System,
                        Name: name,
                        ParentId: "group:system",
                        Sql: sql));
                    continue;
                }

                if (type.Equals("index", StringComparison.OrdinalIgnoreCase) &&
                    name.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var kind = type.ToLowerInvariant() switch
                {
                    "table" => DatabaseObjectKinds.Table,
                    "view" => DatabaseObjectKinds.View,
                    "index" => DatabaseObjectKinds.Index,
                    "trigger" => DatabaseObjectKinds.Trigger,
                    _ => type
                };

                var groupId = kind switch
                {
                    DatabaseObjectKinds.Table => "group:tables",
                    DatabaseObjectKinds.View => "group:views",
                    DatabaseObjectKinds.Index => "group:indexes",
                    DatabaseObjectKinds.Trigger => "group:triggers",
                    _ => $"group:{kind}s"
                };

                nodes.Add(new DatabaseObjectNode(
                    Id: $"{kind}:{name}",
                    Kind: kind,
                    Name: name,
                    ParentId: groupId,
                    ParentObjectName: tableName,
                    Sql: sql));
            }
        }

        return nodes;
    }

    public async Task<string?> GetObjectSqlAsync(
        string filePath,
        string kind,
        string name,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateAsync(filePath, cancellationToken: cancellationToken);
        await connection.OpenAsync(cancellationToken);

        var masterType = kind.Equals(DatabaseObjectKinds.System, StringComparison.OrdinalIgnoreCase)
            ? "table"
            : kind.ToLowerInvariant();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT sql
            FROM sqlite_master
            WHERE type = $type
              AND name = $name
            LIMIT 1;
            """;
        AddParameter(command, "$type", masterType);
        AddParameter(command, "$name", name);
        return (await command.ExecuteScalarAsync(cancellationToken))?.ToString();
    }

    public async Task<TableSchema> GetSchemaAsync(string filePath, string tableName, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateAsync(filePath, cancellationToken: cancellationToken);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""PRAGMA table_info({SqliteIdentifier.Quote(tableName)});""";

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

    public async Task<TableMetadata> GetTableMetadataAsync(string filePath, string tableName, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateAsync(filePath, cancellationToken: cancellationToken);
        await connection.OpenAsync(cancellationToken);

        long rowCount;
        try
        {
            rowCount = await ExecuteScalarAsync<long>(
                connection,
                $"""SELECT COUNT(*) FROM {SqliteIdentifier.Quote(tableName)};""",
                cancellationToken);
        }
        catch
        {
            // Views or special objects may not support COUNT; keep metadata usable.
            rowCount = 0;
        }
        var pageSize = await ExecuteScalarAsync<int>(
            connection,
            "PRAGMA page_size;",
            cancellationToken);
        var sqliteVersion = await ExecuteScalarAsync<string>(
            connection,
            "SELECT sqlite_version();",
            cancellationToken);
        var encoding = await ExecuteScalarAsync<string>(
            connection,
            "PRAGMA encoding;",
            cancellationToken);
        var userVersion = await ExecuteScalarAsync<int>(
            connection,
            "PRAGMA user_version;",
            cancellationToken);

        await using var ddlCommand = connection.CreateCommand();
        // Tables and views both expose schema via PRAGMA; DDL lives under type table|view.
        ddlCommand.CommandText = """
            SELECT sql
            FROM sqlite_master
            WHERE type IN ('table', 'view')
              AND name = $name
            LIMIT 1;
            """;
        AddParameter(ddlCommand, "$name", tableName);
        var createSql = (await ddlCommand.ExecuteScalarAsync(cancellationToken))?.ToString();

        var indexes = new List<DatabaseScriptItem>();
        await using (var indexCommand = connection.CreateCommand())
        {
            indexCommand.CommandText = """
                SELECT name, sql
                FROM sqlite_master
                WHERE type = 'index'
                  AND tbl_name = $tableName
                  AND name NOT LIKE 'sqlite_%'
                ORDER BY name;
                """;
            AddParameter(indexCommand, "$tableName", tableName);

            await using var reader = await indexCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                indexes.Add(new DatabaseScriptItem(
                    reader.GetString(0),
                    "index",
                    reader.IsDBNull(1) ? null : reader.GetString(1)));
            }
        }

        var triggers = new List<DatabaseScriptItem>();
        await using (var triggerCommand = connection.CreateCommand())
        {
            triggerCommand.CommandText = """
                SELECT name, sql
                FROM sqlite_master
                WHERE type = 'trigger'
                  AND tbl_name = $tableName
                ORDER BY name;
                """;
            AddParameter(triggerCommand, "$tableName", tableName);

            await using var reader = await triggerCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                triggers.Add(new DatabaseScriptItem(
                    reader.GetString(0),
                    "trigger",
                    reader.IsDBNull(1) ? null : reader.GetString(1)));
            }
        }

        return new TableMetadata(
            rowCount,
            pageSize,
            sqliteVersion,
            encoding,
            userVersion,
            createSql,
            indexes,
            triggers);
    }

    private static async Task<T> ExecuteScalarAsync<T>(
        DbConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null || result is DBNull)
        {
            return default!;
        }

        return (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void AddParameter(DbCommand command, string parameterName, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = parameterName;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

}
