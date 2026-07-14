namespace Open.Db.Viewer.Domain.Sqlite;

/// <summary>
/// SQLite 标识符转义（表名/列名）。统一使用双引号并转义内部引号。
/// </summary>
public static class SqliteIdentifier
{
    public static string Quote(string identifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        return "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
