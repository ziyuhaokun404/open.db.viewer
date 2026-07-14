using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Domain.Sqlite;

namespace Open.Db.Viewer.Domain.Tests.Models;

public class DomainModelsTests
{
    [Fact]
    public void QueryExecutionResult_DefaultsTruncationFlags()
    {
        var result = new QueryExecutionResult(
            ["id"],
            [new object?[] { 1 }],
            1,
            TimeSpan.FromMilliseconds(1),
            "ok");

        Assert.False(result.IsTruncated);
        Assert.Null(result.MaxRowsLimit);
    }

    [Fact]
    public void AppSettings_Normalize_ClampsAndFixesInvalidValues()
    {
        var settings = new AppSettings
        {
            ThemePreference = "neon",
            DefaultPageSizeValue = 999,
            QueryMaxResultRows = 1,
            QueryTimeoutSeconds = 9999
        };

        settings.Normalize();

        Assert.Equal("System", settings.ThemePreference);
        Assert.Equal(AppSettings.DefaultPageSize, settings.DefaultPageSizeValue);
        Assert.Equal(AppSettings.MinQueryMaxResultRows, settings.QueryMaxResultRows);
        Assert.Equal(AppSettings.MaxQueryTimeoutSeconds, settings.QueryTimeoutSeconds);
    }

    [Fact]
    public void SqliteIdentifier_Quote_EscapesEmbeddedQuotes()
    {
        Assert.Equal("\"weird\"\"table\"", SqliteIdentifier.Quote("weird\"table"));
        Assert.Equal("\"users\"", SqliteIdentifier.Quote("users"));
    }

    [Fact]
    public void DatabaseObjectNode_ExposesBrowseAndSchemaFlags()
    {
        var table = new DatabaseObjectNode("table:users", DatabaseObjectKinds.Table, "users");
        var index = new DatabaseObjectNode("index:idx", DatabaseObjectKinds.Index, "idx", ParentObjectName: "users");
        var group = new DatabaseObjectNode("group:tables", DatabaseObjectKinds.Group, "表");

        Assert.True(table.SupportsDataBrowse);
        Assert.True(DatabaseObjectKinds.IsSchemaLoadable(table.Kind));
        Assert.False(index.SupportsDataBrowse);
        Assert.True(DatabaseObjectKinds.IsSchemaLoadable(index.Kind));
        Assert.Equal("索引 · users", index.Subtitle);
        Assert.True(group.IsGroup);
    }
}
