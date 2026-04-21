using System.Globalization;
using System.Text;
using Open.Db.Viewer.Application.Abstractions;

namespace Open.Db.Viewer.Infrastructure.Sqlite.Export;

public sealed class CsvExportWriter : ICsvExportWriter
{
    public async Task WriteAsync(
        string filePath,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<object?>> rows,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        await writer.WriteLineAsync(string.Join(",", columns.Select(Escape)));

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(string.Join(",", row.Select(value => Escape(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty))));
        }

        await writer.FlushAsync();
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
