using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Domain.Models;
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

    public async Task<TableMetadata> GetTableMetadataAsync(string filePath, string tableName, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateAsync(filePath, cancellationToken);
        await connection.OpenAsync(cancellationToken);

        var rowCount = await ExecuteScalarAsync<long>(
            connection,
            $"""SELECT COUNT(*) FROM {QuoteIdentifier(tableName)};""",
            cancellationToken);
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
        ddlCommand.CommandText = """
            SELECT sql
            FROM sqlite_master
            WHERE type = 'table'
              AND name = $name;
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

    private static string QuoteIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";
}
