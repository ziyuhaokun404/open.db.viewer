namespace Open.Db.Viewer.Application.Abstractions;

public enum SqlStatementCategory
{
    ReadOnly,
    Dml,
    Ddl,
    Maintenance,
    Transaction
}

public static class SqliteStatementClassifier
{
    private static readonly HashSet<string> ReadOnlyKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "EXPLAIN",
        "SELECT",
        "WITH"
    };

    private static readonly HashSet<string> DmlKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "INSERT",
        "UPDATE",
        "DELETE",
        "REPLACE"
    };

    private static readonly HashSet<string> DdlKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "CREATE",
        "ALTER",
        "DROP"
    };

    private static readonly HashSet<string> MaintenanceKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "VACUUM",
        "ANALYZE",
        "REINDEX",
        "ATTACH",
        "DETACH"
    };

    private static readonly HashSet<string> TransactionKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "BEGIN",
        "COMMIT",
        "ROLLBACK",
        "SAVEPOINT",
        "RELEASE",
        "END"
    };

    public static bool IsReadOnly(string sql) =>
        Classify(sql) == SqlStatementCategory.ReadOnly;

    public static SqlStatementCategory Classify(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var keyword = ReadFirstKeyword(sql);

        if (ReadOnlyKeywords.Contains(keyword))
        {
            return SqlStatementCategory.ReadOnly;
        }

        if (string.Equals(keyword, "PRAGMA", StringComparison.OrdinalIgnoreCase))
        {
            return sql.Contains('=', StringComparison.Ordinal)
                ? SqlStatementCategory.Dml
                : SqlStatementCategory.ReadOnly;
        }

        if (DmlKeywords.Contains(keyword))
        {
            return SqlStatementCategory.Dml;
        }

        if (DdlKeywords.Contains(keyword))
        {
            return SqlStatementCategory.Ddl;
        }

        if (MaintenanceKeywords.Contains(keyword))
        {
            return SqlStatementCategory.Maintenance;
        }

        if (TransactionKeywords.Contains(keyword))
        {
            return SqlStatementCategory.Transaction;
        }

        return SqlStatementCategory.Dml;
    }

    public static bool IsHighRisk(string sql) =>
        Classify(sql) is SqlStatementCategory.Ddl or SqlStatementCategory.Maintenance;

    private static string ReadFirstKeyword(string sql)
    {
        var index = SkipTrivia(sql, 0);
        var start = index;

        while (index < sql.Length && (char.IsLetter(sql[index]) || sql[index] == '_'))
        {
            index++;
        }

        return index == start
            ? string.Empty
            : sql[start..index].ToUpperInvariant();
    }

    private static int SkipTrivia(string sql, int index)
    {
        while (index < sql.Length)
        {
            if (char.IsWhiteSpace(sql[index]))
            {
                index++;
                continue;
            }

            if (index + 1 < sql.Length && sql[index] == '-' && sql[index + 1] == '-')
            {
                index += 2;
                while (index < sql.Length && sql[index] is not '\r' and not '\n')
                {
                    index++;
                }

                continue;
            }

            if (index + 1 < sql.Length && sql[index] == '/' && sql[index + 1] == '*')
            {
                index += 2;
                while (index + 1 < sql.Length && (sql[index] != '*' || sql[index + 1] != '/'))
                {
                    index++;
                }

                index = Math.Min(index + 2, sql.Length);
                continue;
            }

            break;
        }

        return index;
    }
}
