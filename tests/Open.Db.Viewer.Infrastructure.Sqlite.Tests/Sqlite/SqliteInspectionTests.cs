using FluentAssertions;
using Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;
using Open.Db.Viewer.Infrastructure.Sqlite.Tests.Support;

namespace Open.Db.Viewer.Infrastructure.Sqlite.Tests.Sqlite;

public class SqliteInspectionTests
{
    [Fact]
    public async Task GetTablesAsync_ReturnsSeededTables()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var factory = new SqliteConnectionFactory();
        var inspector = new SqliteDatabaseInspector(factory);

        var tables = await inspector.GetTablesAsync(db.FilePath);

        tables.Should().Equal("orders", "users");
    }

    [Fact]
    public async Task GetSchemaAsync_ReturnsPrimaryKeyMetadata()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var factory = new SqliteConnectionFactory();
        var inspector = new SqliteDatabaseInspector(factory);

        var schema = await inspector.GetSchemaAsync(db.FilePath, "users");

        schema.TableName.Should().Be("users");
        schema.Columns.Should().ContainSingle(column =>
            column.Name == "id" &&
            column.IsPrimaryKey &&
            column.DataType == "INTEGER" &&
            !column.IsNullable);
    }

    [Fact]
    public async Task GetTableMetadataAsync_ReturnsCountsAndScripts()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var factory = new SqliteConnectionFactory();
        var inspector = new SqliteDatabaseInspector(factory);

        var metadata = await inspector.GetTableMetadataAsync(db.FilePath, "orders");

        metadata.RowCount.Should().Be(2);
        metadata.PageSizeBytes.Should().BePositive();
        metadata.SqliteVersion.Should().NotBeNullOrWhiteSpace();
        metadata.Encoding.Should().NotBeNullOrWhiteSpace();
        metadata.UserVersion.Should().Be(0);
        metadata.CreateSql.Should().Contain("CREATE TABLE orders");
        metadata.Indexes.Should().ContainSingle(index => index.Name == "idx_orders_user_id");
        metadata.Triggers.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadPageAsync_ReturnsRequestedPageAndHasNextPageWhenMoreRowsExist()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var factory = new SqliteConnectionFactory();
        var reader = new SqliteTableDataReader(factory);

        var page = await reader.ReadPageAsync(db.FilePath, "users", 1, 2, "id", "ASC");

        page.PageNumber.Should().Be(1);
        page.PageSize.Should().Be(2);
        page.SortColumn.Should().Be("id");
        page.SortDirection.Should().Be("ASC");
        page.Rows.Should().HaveCount(2);
        page.Rows[0][0].Should().Be(1);
        page.Rows[0][1].Should().Be("Alice");
        page.Rows[1][0].Should().Be(2);
        page.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ReadPageAsync_ReturnsHasNextPageFalseForFinalPage()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var factory = new SqliteConnectionFactory();
        var reader = new SqliteTableDataReader(factory);

        var page = await reader.ReadPageAsync(db.FilePath, "users", 2, 2, "id", "ASC");

        page.Rows.Should().HaveCount(1);
        page.Rows[0][0].Should().Be(3);
        page.Rows[0][1].Should().Be("Charlie");
        page.HasNextPage.Should().BeFalse();
    }
}
