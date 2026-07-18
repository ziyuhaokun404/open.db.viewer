namespace Sqlite.Viewer.Application.Abstractions;

public enum SqlStatementCategory
{
    ReadOnly,
    Dml,
    Ddl,
    /// <summary>PRAGMA 赋值类写操作（如 journal_mode=WAL）。</summary>
    PragmaWrite,
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
            return IsPragmaAssignment(sql)
                ? SqlStatementCategory.PragmaWrite
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

        // 未知关键字默认按写操作对待，避免误放行。
        return SqlStatementCategory.Dml;
    }

    /// <summary>
    /// 高风险：DDL / 维护 / 写 PRAGMA。可写模式下默认需确认。
    /// </summary>
    public static bool IsHighRisk(string sql) =>
        Classify(sql) is SqlStatementCategory.Ddl
            or SqlStatementCategory.Maintenance
            or SqlStatementCategory.PragmaWrite;

    public static string GetCategoryDisplayName(SqlStatementCategory category) => category switch
    {
        SqlStatementCategory.ReadOnly => "只读查询",
        SqlStatementCategory.Dml => "DML（数据修改）",
        SqlStatementCategory.Ddl => "DDL（数据定义）",
        SqlStatementCategory.PragmaWrite => "PRAGMA 写操作",
        SqlStatementCategory.Maintenance => "数据库维护",
        SqlStatementCategory.Transaction => "事务控制",
        _ => category.ToString()
    };

    /// <summary>
    /// 粗略判断 PRAGMA 是否为赋值语句（含 '='）。
    /// 忽略注释后的主体；字符串内的 '=' 极少用于 PRAGMA 赋值名，可接受误判为写。
    /// </summary>
    private static bool IsPragmaAssignment(string sql)
    {
        var index = SkipTrivia(sql, 0);
        // 跳过 PRAGMA 关键字
        while (index < sql.Length && (char.IsLetter(sql[index]) || sql[index] == '_'))
        {
            index++;
        }

        index = SkipTrivia(sql, index);
        var inSingleQuote = false;
        for (; index < sql.Length; index++)
        {
            var ch = sql[index];
            if (ch == '\'')
            {
                if (inSingleQuote && index + 1 < sql.Length && sql[index + 1] == '\'')
                {
                    index++;
                    continue;
                }

                inSingleQuote = !inSingleQuote;
                continue;
            }

            if (ch == '=' && !inSingleQuote)
            {
                return true;
            }
        }

        return false;
    }

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
