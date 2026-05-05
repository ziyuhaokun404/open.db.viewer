namespace Open.Db.Viewer.Application.Abstractions;

public static class SqliteStatementClassifier
{
    private static readonly HashSet<string> ReadOnlyLeadingKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "EXPLAIN",
        "SELECT",
        "WITH"
    };

    public static bool IsReadOnly(string sql)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        var keyword = ReadFirstKeyword(sql);
        if (ReadOnlyLeadingKeywords.Contains(keyword))
        {
            return true;
        }

        return string.Equals(keyword, "PRAGMA", StringComparison.OrdinalIgnoreCase) &&
               !sql.Contains('=', StringComparison.Ordinal);
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
