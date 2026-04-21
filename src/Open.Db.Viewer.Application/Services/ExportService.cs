using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Domain.Models;

namespace Open.Db.Viewer.Application.Services;

public sealed class ExportService
{
    private readonly ICsvExportWriter _csvExportWriter;

    public ExportService(ICsvExportWriter csvExportWriter)
    {
        _csvExportWriter = csvExportWriter;
    }

    public Task ExportAsync(
        string filePath,
        TabularData data,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        return _csvExportWriter.WriteAsync(filePath, data.Columns, data.Rows, cancellationToken);
    }
}
