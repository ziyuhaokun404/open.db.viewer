using Sqlite.Viewer.Application.Abstractions;
using Sqlite.Viewer.Domain.Display;
using System.Globalization;
using System.Text;

namespace Sqlite.Viewer.Infrastructure.Sqlite.Export;

public sealed class CsvExportWriter : ICsvExportWriter
{
    public Task WriteAsync(
        string filePath,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<object?>> rows,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return WriteStreamingAsync(
            filePath,
            columns,
            ToAsyncEnumerable(rows),
            rowsWrittenProgress: null,
            cancellationToken);
    }

    public async Task WriteStreamingAsync(
        string filePath,
        IReadOnlyList<string> columns,
        IAsyncEnumerable<IReadOnlyList<object?>> rows,
        IProgress<long>? rowsWrittenProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);

        await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        await writer.WriteLineAsync(string.Join(",", columns.Select(Escape)));

        long written = 0;
        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(string.Join(",", row.Select(value => Escape(CellDisplayFormatter.FormatForExport(value)))));
            written++;
            if (written % 100 == 0)
            {
                rowsWrittenProgress?.Report(written);
            }
        }

        rowsWrittenProgress?.Report(written);
        await writer.FlushAsync(cancellationToken);
    }

    private static async IAsyncEnumerable<IReadOnlyList<object?>> ToAsyncEnumerable(
        IReadOnlyList<IReadOnlyList<object?>> rows)
    {
        foreach (var row in rows)
        {
            yield return row;
        }

        await Task.CompletedTask;
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
