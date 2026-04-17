using System.Text;
using FluentAssertions;
using OpenDbViewer.Infrastructure.Sqlite.Export;

namespace OpenDbViewer.Infrastructure.Sqlite.Tests.Export;

public class CsvExportWriterTests
{
    [Fact]
    public async Task WriteAsync_EscapesSpecialCharacters_AndWritesUtf8Bom()
    {
        var targetPath = Path.Combine(Path.GetTempPath(), $"open-db-viewer-csv-{Guid.NewGuid():N}.csv");
        var writer = new CsvExportWriter();

        try
        {
            await writer.WriteAsync(
                targetPath,
                new[] { "Name", "Note" },
                new[]
                {
                    new object?[] { "Alice, \"Admin\"", "Line 1\nLine 2" }
                });

            var bytes = await File.ReadAllBytesAsync(targetPath);
            bytes.Should().StartWith(new byte[] { 0xEF, 0xBB, 0xBF });

            var content = Encoding.UTF8.GetString(bytes[3..]);
            content.Should().Be("Name,Note\r\n\"Alice, \"\"Admin\"\"\",\"Line 1\nLine 2\"\r\n");
        }
        finally
        {
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }
        }
    }
}
