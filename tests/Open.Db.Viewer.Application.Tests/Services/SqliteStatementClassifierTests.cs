using FluentAssertions;
using Open.Db.Viewer.Application.Abstractions;

namespace Open.Db.Viewer.Application.Tests.Services;

public class SqliteStatementClassifierTests
{
    [Theory]
    [InlineData("select * from users")]
    [InlineData("SELECT * FROM users")]
    [InlineData("SELECT 1")]
    [InlineData("explain query plan select * from users")]
    [InlineData("EXPLAIN select 1")]
    [InlineData("with cte as (select 1) select * from cte")]
    [InlineData("WITH recursive cte AS (SELECT 1) SELECT * FROM cte")]
    public void Classify_ShouldReturnReadOnly(string sql)
    {
        SqliteStatementClassifier.Classify(sql).Should().Be(SqlStatementCategory.ReadOnly);
    }

    [Theory]
    [InlineData("pragma table_info(users)")]
    [InlineData("PRAGMA encoding")]
    [InlineData("pragma page_size")]
    public void Classify_ShouldReturnReadOnly_ForReadOnlyPragmas(string sql)
    {
        SqliteStatementClassifier.Classify(sql).Should().Be(SqlStatementCategory.ReadOnly);
    }

    [Theory]
    [InlineData("pragma journal_mode = WAL")]
    [InlineData("PRAGMA user_version = 5")]
    [InlineData("pragma busy_timeout=3000")]
    public void Classify_ShouldReturnDml_ForPragmasWithAssignment(string sql)
    {
        SqliteStatementClassifier.Classify(sql).Should().Be(SqlStatementCategory.Dml);
    }

    [Theory]
    [InlineData("insert into users (name) values ('Alice')")]
    [InlineData("INSERT INTO users (name) VALUES ('Bob')")]
    [InlineData("update users set name = 'X'")]
    [InlineData("UPDATE users SET name = 'X' WHERE id = 1")]
    [InlineData("delete from users")]
    [InlineData("DELETE FROM users WHERE id = 1")]
    [InlineData("replace into users (id, name) values (1, 'A')")]
    [InlineData("REPLACE INTO users (id, name) VALUES (1, 'A')")]
    public void Classify_ShouldReturnDml(string sql)
    {
        SqliteStatementClassifier.Classify(sql).Should().Be(SqlStatementCategory.Dml);
    }

    [Theory]
    [InlineData("create table test (x integer)")]
    [InlineData("CREATE TABLE test (x integer)")]
    [InlineData("alter table users add column age integer")]
    [InlineData("ALTER TABLE users RENAME TO people")]
    [InlineData("drop table test")]
    [InlineData("DROP TABLE IF EXISTS test")]
    public void Classify_ShouldReturnDdl(string sql)
    {
        SqliteStatementClassifier.Classify(sql).Should().Be(SqlStatementCategory.Ddl);
    }

    [Theory]
    [InlineData("vacuum")]
    [InlineData("VACUUM")]
    [InlineData("analyze")]
    [InlineData("ANALYZE users")]
    [InlineData("reindex")]
    [InlineData("REINDEX")]
    [InlineData("attach database 'test.db' as test")]
    [InlineData("ATTACH 'test.db' AS test")]
    [InlineData("detach test")]
    [InlineData("DETACH DATABASE test")]
    public void Classify_ShouldReturnMaintenance(string sql)
    {
        SqliteStatementClassifier.Classify(sql).Should().Be(SqlStatementCategory.Maintenance);
    }

    [Theory]
    [InlineData("begin")]
    [InlineData("BEGIN")]
    [InlineData("BEGIN TRANSACTION")]
    [InlineData("commit")]
    [InlineData("COMMIT")]
    [InlineData("rollback")]
    [InlineData("ROLLBACK")]
    [InlineData("savepoint sp1")]
    [InlineData("release sp1")]
    [InlineData("RELEASE SAVEPOINT sp1")]
    [InlineData("end")]
    [InlineData("END TRANSACTION")]
    public void Classify_ShouldReturnTransaction(string sql)
    {
        SqliteStatementClassifier.Classify(sql).Should().Be(SqlStatementCategory.Transaction);
    }

    [Theory]
    [InlineData("-- this is a comment\nselect * from users")]
    [InlineData("/* block comment */ SELECT * FROM users")]
    [InlineData("   \n  \t SELECT * FROM users")]
    [InlineData("-- line comment\n-- another\nWITH cte AS (SELECT 1) SELECT * FROM cte")]
    public void Classify_ShouldSkipTrivia(string sql)
    {
        SqliteStatementClassifier.Classify(sql).Should().Be(SqlStatementCategory.ReadOnly);
    }

    [Theory]
    [InlineData("create table test (x integer)", true)]
    [InlineData("vacuum", true)]
    [InlineData("analyze", true)]
    [InlineData("drop table test", true)]
    [InlineData("alter table users add column x int", true)]
    [InlineData("insert into users values (1)", false)]
    [InlineData("update users set x = 1", false)]
    [InlineData("delete from users", false)]
    [InlineData("select * from users", false)]
    public void IsHighRisk_ShouldReturnTrue_ForDdlAndMaintenance(string sql, bool expected)
    {
        SqliteStatementClassifier.IsHighRisk(sql).Should().Be(expected);
    }

    [Fact]
    public void IsReadOnly_ShouldReturnTrueOnlyForReadOnlyStatements()
    {
        SqliteStatementClassifier.IsReadOnly("select * from users").Should().BeTrue();
        SqliteStatementClassifier.IsReadOnly("insert into users values (1)").Should().BeFalse();
        SqliteStatementClassifier.IsReadOnly("create table t(x int)").Should().BeFalse();
        SqliteStatementClassifier.IsReadOnly("vacuum").Should().BeFalse();
    }
}
