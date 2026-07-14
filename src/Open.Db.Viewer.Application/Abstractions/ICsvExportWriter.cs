namespace Open.Db.Viewer.Application.Abstractions;

public interface ICsvExportWriter
{
    Task WriteAsync(
        string filePath,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<object?>> rows,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 流式写出：先写表头，再按异步序列写行。适合整表导出。
    /// </summary>
    Task WriteStreamingAsync(
        string filePath,
        IReadOnlyList<string> columns,
        IAsyncEnumerable<IReadOnlyList<object?>> rows,
        IProgress<long>? rowsWrittenProgress = null,
        CancellationToken cancellationToken = default);
}
