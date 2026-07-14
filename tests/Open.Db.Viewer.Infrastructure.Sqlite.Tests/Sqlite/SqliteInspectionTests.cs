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
    public async Task GetObjectCatalogAsync_ReturnsTablesViewsIndexesTriggers()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var inspector = new SqliteDatabaseInspector(new SqliteConnectionFactory());

        var catalog = await inspector.GetObjectCatalogAsync(db.FilePath);

        catalog.Should().Contain(node => node.Kind == "table" && node.Name == "users");
        catalog.Should().Contain(node => node.Kind == "view" && node.Name == "user_names");
        catalog.Should().Contain(node => node.Kind == "index" && node.Name == "idx_orders_user_id");
        catalog.Should().Contain(node => node.Kind == "trigger" && node.Name == "trg_users_after_insert");
        catalog.Should().NotContain(node => node.Name.StartsWith("sqlite_"));
    }

    [Fact]
    public async Task GetObjectCatalogAsync_CanIncludeSystemTables()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var inspector = new SqliteDatabaseInspector(new SqliteConnectionFactory());

        var catalog = await inspector.GetObjectCatalogAsync(db.FilePath, includeSystemObjects: true);

        catalog.Should().Contain(node => node.Kind == "system" && node.Name.StartsWith("sqlite_"));
    }

    [Fact]
    public async Task GetObjectSqlAsync_ReturnsIndexDdl()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var inspector = new SqliteDatabaseInspector(new SqliteConnectionFactory());

        var sql = await inspector.GetObjectSqlAsync(db.FilePath, "index", "idx_orders_user_id");

        sql.Should().Contain("CREATE INDEX");
        sql.Should().Contain("orders");
    }

    [Fact]
    public async Task GetTableMetadataAsync_ReturnsViewCreateSql()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var inspector = new SqliteDatabaseInspector(new SqliteConnectionFactory());

        var metadata = await inspector.GetTableMetadataAsync(db.FilePath, "user_names");

        metadata.CreateSql.Should().Contain("CREATE VIEW");
        metadata.RowCount.Should().Be(3);
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
        page.TotalRowCount.Should().Be(3);
        page.TotalPages.Should().Be(2);
        page.Rows.Should().HaveCount(2);
        Convert.ToInt64(page.Rows[0][0]).Should().Be(1);
        page.Rows[0][1].Should().Be("Alice");
        Convert.ToInt64(page.Rows[1][0]).Should().Be(2);
        page.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task ReadPageAsync_AppliesContainsFilter()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var reader = new SqliteTableDataReader(new SqliteConnectionFactory());

        var page = await reader.ReadPageAsync(
            db.FilePath,
            "users",
            pageNumber: 1,
            pageSize: 10,
            sortColumn: "id",
            sortDirection: "ASC",
            filters: [new Domain.Models.TableFilter("name", Domain.Models.TableFilterOperator.Contains, "o")]);

        page.TotalRowCount.Should().Be(1);
        page.Rows.Should().ContainSingle();
        page.Rows[0][1].Should().Be("Bob");
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
