using FluentAssertions;
using Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;
using Open.Db.Viewer.Infrastructure.Sqlite.Tests.Support;

namespace Open.Db.Viewer.Infrastructure.Sqlite.Tests.Sqlite;

public class SqliteQueryExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsColumnsAndRows_ForSelectQuery()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var executor = new SqliteQueryExecutor(new SqliteConnectionFactory());

        var result = await executor.ExecuteAsync(db.FilePath, "select id, name from users order by id");

        result.Columns.Should().ContainInOrder("id", "name");
        result.Rows.Should().HaveCount(3);
        result.Rows[0].Should().ContainInOrder(1L, "Alice");
        result.Rows[2].Should().ContainInOrder(3L, "Charlie");
    }
}
