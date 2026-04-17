namespace OpenDbViewer.Application.Abstractions;

public interface ICsvExportWriter
{
    Task WriteAsync(
        string filePath,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<object?>> rows,
        CancellationToken cancellationToken = default);
}
