using FluentAssertions;
using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;

namespace Open.Db.Viewer.Application.Tests.Services;

public class ExportServiceTests
{
    [Fact]
    public async Task ExportAsync_ForwardsTabularData_ToTheWriter()
    {
        var writer = new RecordingCsvExportWriter();
        var service = new ExportService(writer);
        var data = new TabularData(
            new[] { "Name" },
            new[] { new object?[] { "Alice" } });

        await service.ExportAsync(@"C:\data\export.csv", data);

        writer.FilePath.Should().Be(@"C:\data\export.csv");
        writer.Columns.Should().ContainSingle().Which.Should().Be("Name");
        writer.Rows.Should().ContainSingle().Which[0].Should().Be("Alice");
    }

    private sealed class RecordingCsvExportWriter : ICsvExportWriter
    {
        public string? FilePath { get; private set; }
        public IReadOnlyList<string>? Columns { get; private set; }
        public IReadOnlyList<IReadOnlyList<object?>>? Rows { get; private set; }

        public Task WriteAsync(
            string filePath,
            IReadOnlyList<string> columns,
            IReadOnlyList<IReadOnlyList<object?>> rows,
            CancellationToken cancellationToken = default)
        {
            FilePath = filePath;
            Columns = columns;
            Rows = rows;

            return Task.CompletedTask;
        }
    }
}
