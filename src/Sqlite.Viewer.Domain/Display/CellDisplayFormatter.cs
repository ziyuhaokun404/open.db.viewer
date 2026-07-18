using System.Globalization;
using System.Text;

namespace Sqlite.Viewer.Domain.Display;

/// <summary>
/// 网格展示用的单元格格式化（不影响导出/复制的原始值路径）。
/// </summary>
public static class CellDisplayFormatter
{
    public const string NullMarker = "(NULL)";
    public const int DefaultMaxTextLength = 200;

    public static string Format(object? value, int maxTextLength = DefaultMaxTextLength)
    {
        if (value is null || value is DBNull)
        {
            return NullMarker;
        }

        if (value is byte[] bytes)
        {
            return $"BLOB ({bytes.Length} 字节)";
        }

        var text = value switch
        {
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };

        if (maxTextLength > 0 && text.Length > maxTextLength)
        {
            return text[..maxTextLength] + "…";
        }

        return text;
    }

    public static string FormatForExport(object? value)
    {
        if (value is null || value is DBNull)
        {
            return string.Empty;
        }

        if (value is byte[] bytes)
        {
            return Convert.ToHexString(bytes);
        }

        return value switch
        {
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    public static string ToTsv(IReadOnlyList<object?> values)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('\t');
            }

            builder.Append(EscapeTsv(FormatForExport(values[i])));
        }

        return builder.ToString();
    }

    public static string ToTsv(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<object?>> rows)
    {
        var builder = new StringBuilder();
        builder.Append(string.Join('\t', headers.Select(EscapeTsv)));
        foreach (var row in rows)
        {
            builder.AppendLine();
            builder.Append(ToTsv(row));
        }

        return builder.ToString();
    }

    private static string EscapeTsv(string value)
    {
        if (value.Contains('\t') || value.Contains('\n') || value.Contains('\r') || value.Contains('"'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
