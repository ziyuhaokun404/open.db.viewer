namespace Open.Db.Viewer.Application.Sql;

/// <summary>
/// 基于分号拆分 SQL，忽略字符串、引用标识符和注释内的分号。
/// </summary>
public static class SqlStatementSplitter
{
    public static string ResolveStatementToExecute(string fullText, int caretIndex = -1)
    {
        if (string.IsNullOrWhiteSpace(fullText))
        {
            return string.Empty;
        }

        var statements = Split(fullText);
        if (statements.Count == 0)
        {
            return fullText.Trim();
        }

        if (caretIndex < 0 || caretIndex > fullText.Length)
        {
            return statements[^1].Text.Trim();
        }

        foreach (var statement in statements)
        {
            if (caretIndex >= statement.Start && caretIndex <= statement.End)
            {
                return statement.Text.Trim();
            }
        }

        return statements[^1].Text.Trim();
    }

    public static IReadOnlyList<SqlStatementSpan> Split(string sql)
    {
        var spans = new List<SqlStatementSpan>();
        if (string.IsNullOrEmpty(sql))
        {
            return spans;
        }

        var state = ParserState.Normal;
        var start = 0;
        for (var i = 0; i < sql.Length; i++)
        {
            var ch = sql[i];
            var next = i + 1 < sql.Length ? sql[i + 1] : '\0';

            switch (state)
            {
                case ParserState.LineComment:
                    if (ch is '\r' or '\n')
                    {
                        state = ParserState.Normal;
                    }
                    continue;
                case ParserState.BlockComment:
                    if (ch == '*' && next == '/')
                    {
                        state = ParserState.Normal;
                        i++;
                    }
                    continue;
                case ParserState.SingleQuote:
                    if (ch == '\'' && next == '\'')
                    {
                        i++;
                    }
                    else if (ch == '\'')
                    {
                        state = ParserState.Normal;
                    }
                    continue;
                case ParserState.DoubleQuote:
                    if (ch == '"' && next == '"')
                    {
                        i++;
                    }
                    else if (ch == '"')
                    {
                        state = ParserState.Normal;
                    }
                    continue;
                case ParserState.Backtick:
                    if (ch == '`' && next == '`')
                    {
                        i++;
                    }
                    else if (ch == '`')
                    {
                        state = ParserState.Normal;
                    }
                    continue;
                case ParserState.Bracket:
                    if (ch == ']')
                    {
                        state = ParserState.Normal;
                    }
                    continue;
            }

            if (ch == '-' && next == '-')
            {
                state = ParserState.LineComment;
                i++;
            }
            else if (ch == '/' && next == '*')
            {
                state = ParserState.BlockComment;
                i++;
            }
            else if (ch == '\'')
            {
                state = ParserState.SingleQuote;
            }
            else if (ch == '"')
            {
                state = ParserState.DoubleQuote;
            }
            else if (ch == '`')
            {
                state = ParserState.Backtick;
            }
            else if (ch == '[')
            {
                state = ParserState.Bracket;
            }
            else if (ch == ';')
            {
                AddSpan(spans, sql, start, i + 1);
                start = i + 1;
            }
        }

        if (start < sql.Length)
        {
            var tail = sql[start..];
            if (!string.IsNullOrWhiteSpace(tail))
            {
                spans.Add(new SqlStatementSpan(start, sql.Length, tail));
            }
        }

        return spans;
    }

    private static void AddSpan(List<SqlStatementSpan> spans, string sql, int start, int end)
    {
        var text = sql[start..end];
        if (!string.IsNullOrWhiteSpace(text.Trim().TrimEnd(';')))
        {
            spans.Add(new SqlStatementSpan(start, end, text));
        }
    }

    private enum ParserState
    {
        Normal,
        SingleQuote,
        DoubleQuote,
        Backtick,
        Bracket,
        LineComment,
        BlockComment
    }

    public readonly record struct SqlStatementSpan(int Start, int End, string Text);
}
