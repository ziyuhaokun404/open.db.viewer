using FluentAssertions;
using Sqlite.Viewer.Infrastructure.Sqlite.Sqlite;
using Sqlite.Viewer.Infrastructure.Sqlite.Tests.Support;

namespace Sqlite.Viewer.Infrastructure.Sqlite.Tests.Sqlite;

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
        result.IsTruncated.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectWriteSql_WhenAllowWriteIsFalse()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var executor = new SqliteQueryExecutor(new SqliteConnectionFactory());

        var act = () => executor.ExecuteAsync(db.FilePath, "delete from users where id = 1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*只读*");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseWritableConnection_WhenAllowWriteIsTrue()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var executor = new SqliteQueryExecutor(new SqliteConnectionFactory());

        var writeResult = await executor.ExecuteAsync(
            db.FilePath,
            "delete from users where id = 1",
            allowWrite: true);
        var readResult = await executor.ExecuteAsync(
            db.FilePath,
            "select count(*) from users");

        writeResult.AffectedRows.Should().Be(1);
        readResult.Rows.Should().ContainSingle()
            .Which.Should().ContainSingle()
            .Which.Should().Be(2L);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTruncateResult_WhenMaxResultRowsIsExceeded()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var executor = new SqliteQueryExecutor(new SqliteConnectionFactory());

        var result = await executor.ExecuteAsync(
            db.FilePath,
            "select id, name from users order by id",
            maxResultRows: 2);

        result.Rows.Should().HaveCount(2);
        result.IsTruncated.Should().BeTrue();
        result.MaxRowsLimit.Should().Be(2);
        result.Message.Should().Contain("截断");
    }
}
