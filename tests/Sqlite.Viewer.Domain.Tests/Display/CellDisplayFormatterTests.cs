using Sqlite.Viewer.Domain.Display;

namespace Sqlite.Viewer.Domain.Tests.Display;

public class CellDisplayFormatterTests
{
    [Fact]
    public void Format_MarksNullAndBlobAndTruncatesLongText()
    {
        Assert.Equal("(NULL)", CellDisplayFormatter.Format(null));
        Assert.Equal("(NULL)", CellDisplayFormatter.Format(DBNull.Value));
        Assert.Equal("BLOB (3 字节)", CellDisplayFormatter.Format(new byte[] { 1, 2, 3 }));
        Assert.Equal("abc…", CellDisplayFormatter.Format("abcdef", maxTextLength: 3));
    }

    [Fact]
    public void ToTsv_JoinsWithTabsAndHeaders()
    {
        var tsv = CellDisplayFormatter.ToTsv(
            ["id", "name"],
            [new object?[] { 1, "Alice" }, new object?[] { 2, null }]);

        Assert.Contains("id\tname", tsv);
        Assert.Contains("1\tAlice", tsv);
        Assert.Contains("2\t", tsv);
    }

    [Fact]
    public void FormatForExport_UsesHexForBlobAndEmptyForNull()
    {
        Assert.Equal(string.Empty, CellDisplayFormatter.FormatForExport(null));
        Assert.Equal("0102", CellDisplayFormatter.FormatForExport(new byte[] { 1, 2 }));
    }
}
