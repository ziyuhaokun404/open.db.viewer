using FluentAssertions;
using Open.Db.Viewer.Application.Sql;

namespace Open.Db.Viewer.Application.Tests.Sql;

public class SqlStatementSplitterTests
{
    [Fact]
    public void ResolveStatementToExecute_UsesStatementUnderCaret()
    {
        var sql = "select 1;\nselect 2;\nselect 3;";
        var caret = sql.IndexOf("select 2", StringComparison.Ordinal) + 3;

        var statement = SqlStatementSplitter.ResolveStatementToExecute(sql, caret);

        statement.Should().Be("select 2;");
    }

    [Fact]
    public void ResolveStatementToExecute_IgnoresSemicolonInsideString()
    {
        var sql = "select 'a;b' as v; select 2;";
        var caret = 5;

        var statement = SqlStatementSplitter.ResolveStatementToExecute(sql, caret);

        statement.Should().Be("select 'a;b' as v;");
    }

    [Fact]
    public void ResolveStatementToExecute_UsesLastStatementWhenCaretMissing()
    {
        var sql = "select 1; select 2;";

        SqlStatementSplitter.ResolveStatementToExecute(sql).Should().Be("select 2;");
    }
}
