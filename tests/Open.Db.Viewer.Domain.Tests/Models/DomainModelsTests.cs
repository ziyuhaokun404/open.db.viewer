using Open.Db.Viewer.Domain.Models;

namespace Open.Db.Viewer.Domain.Tests.Models;

public class DomainModelsTests
{
    [Fact]
    public void DatabaseEntry_CreatePinned_MarksEntryPinnedAndPreservesIdentity()
    {
        var entry = DatabaseEntry.CreatePinned("Demo", @"C:\data\demo.db");

        Assert.Equal("Demo", entry.Name);
        Assert.Equal(@"C:\data\demo.db", entry.FilePath);
        Assert.True(entry.IsPinned);
    }

    [Fact]
    public void TablePageResult_ExposesRowsAndPaginationFields()
    {
        var rows = new IReadOnlyList<object?>[]
        {
            new object?[] { 1, "Alice" }
        };

        var result = new TablePageResult(
            new[] { "Id", "Name" },
            rows,
            2,
            100,
            true,
            "Id",
            "ASC");

        Assert.Equal(new[] { "Id", "Name" }, result.Columns);
        Assert.Equal(rows, result.Rows);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(100, result.PageSize);
        Assert.True(result.HasNextPage);
        Assert.Equal("Id", result.SortColumn);
        Assert.Equal("ASC", result.SortDirection);
    }
}
