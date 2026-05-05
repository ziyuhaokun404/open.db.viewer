using FluentAssertions;

using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.ShellHost.Services;

namespace Open.Db.Viewer.Shell.Tests.ViewModels;

public class QueryViewModelTests
{
    [Fact]
    public async Task ExecuteQueryAsync_ShouldPopulateResultGridAndStatus()
    {
        var queryExecutor = new FakeSqliteQueryExecutor(
            new QueryExecutionResult(
                Columns: ["id", "name"],
                Rows:
                [
                    new object?[] { 1, "Alice" },
                    new object?[] { 2, "Bob" }
                ],
                AffectedRows: 2,
                Duration: TimeSpan.FromMilliseconds(42),
                Message: "查询返回了 2 行。"));
        var viewModel = CreateViewModel(
            new QueryService(queryExecutor),
            exportService: new ExportService(new FakeCsvExportWriter()),
            fileDialogService: new FakeFileDialogService(@"C:\exports\result.csv"));

        viewModel.Configure(@"C:\data\sample.db", "users");

        await viewModel.ExecuteQueryAsync();

        queryExecutor.LastFilePath.Should().Be(@"C:\data\sample.db");
        queryExecutor.LastSql.Should().Be("select * from \"users\" limit 100;");
        queryExecutor.LastAllowWrite.Should().BeFalse();
        viewModel.Columns.Should().Equal("id", "name");
        viewModel.Rows.Should().HaveCount(2);
        viewModel.Rows[0].Values.Should().Equal(1, "Alice");
        viewModel.StatusMessage.Should().Contain("查询返回了 2 行。");
        viewModel.StatusMessage.Should().Contain("42");
    }

    [Fact]
    public async Task ExportResultsAsync_ShouldWriteCurrentQueryResultToSelectedPath()
    {
        var exportWriter = new FakeCsvExportWriter();
        var viewModel = CreateViewModel(
            new QueryService(new FakeSqliteQueryExecutor(
                new QueryExecutionResult(
                    Columns: ["id"],
                    Rows: [new object?[] { 10 }],
                    AffectedRows: 1,
                    Duration: TimeSpan.FromMilliseconds(5),
                    Message: "查询返回了 1 行。"))),
            new ExportService(exportWriter),
            new FakeFileDialogService(@"C:\exports\users.csv"));

        viewModel.Configure(@"C:\data\sample.db", "users");
        await viewModel.ExecuteQueryAsync();

        await viewModel.ExportResultsAsync();

        exportWriter.LastFilePath.Should().Be(@"C:\exports\users.csv");
        exportWriter.LastColumns.Should().Equal("id");
        exportWriter.LastRows.Should().ContainSingle();
        exportWriter.LastRows[0].Should().Equal(10);
        viewModel.StatusMessage.Should().Contain("users.csv");
    }

    [Fact]
    public void TemplateCommands_ShouldReplaceQueryTextForCurrentTable()
    {
        var viewModel = CreateViewModel(
            new QueryService(new FakeSqliteQueryExecutor(
                new QueryExecutionResult(Array.Empty<string>(), Array.Empty<IReadOnlyList<object?>>(), 0, TimeSpan.Zero, string.Empty))),
            new ExportService(new FakeCsvExportWriter()),
            new FakeFileDialogService(null));

        viewModel.Configure(@"C:\data\sample.db", "users");

        viewModel.UseCountTemplateCommand.Execute(null);
        viewModel.QueryText.Should().Be("select count(*) as total_rows from \"users\";");

        viewModel.UseSchemaTemplateCommand.Execute(null);
        viewModel.QueryText.Should().Be("pragma table_info(\"users\");");

        viewModel.UseSelectTemplateCommand.Execute(null);
        viewModel.QueryText.Should().Be("select * from \"users\" limit 100;");
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldSurfaceFailureMessageAndClearResults()
    {
        var viewModel = CreateViewModel(
            new QueryService(new ThrowingSqliteQueryExecutor("SQL error near FROM")),
            new ExportService(new FakeCsvExportWriter()),
            new FakeFileDialogService(null));

        viewModel.Configure(@"C:\data\sample.db", "users");

        await viewModel.ExecuteQueryAsync();

        viewModel.HasResults.Should().BeFalse();
        viewModel.Rows.Should().BeEmpty();
        viewModel.StatusMessage.Should().Be("SQL error near FROM");
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldExposeEmptyResultState_WhenQueryReturnsNoRows()
    {
        var viewModel = CreateViewModel(
            new QueryService(new FakeSqliteQueryExecutor(
                new QueryExecutionResult(
                    Columns: ["id"],
                    Rows: Array.Empty<IReadOnlyList<object?>>(),
                    AffectedRows: 0,
                    Duration: TimeSpan.FromMilliseconds(7),
                    Message: "查询返回了 0 行。"))),
            new ExportService(new FakeCsvExportWriter()),
            new FakeFileDialogService(null));

        viewModel.Configure(@"C:\data\sample.db", "users");
        await viewModel.ExecuteQueryAsync();

        viewModel.HasResults.Should().BeFalse();
        viewModel.ResultSummary.Should().Be("未返回任何数据行。");
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldBlockWriteSql_WhenReadOnlyModeIsActive()
    {
        var queryExecutor = new FakeSqliteQueryExecutor(
            new QueryExecutionResult(
                Array.Empty<string>(),
                Array.Empty<IReadOnlyList<object?>>(),
                0,
                TimeSpan.Zero,
                "should not execute"));
        var viewModel = CreateViewModel(
            new QueryService(queryExecutor),
            exportService: new ExportService(new FakeCsvExportWriter()),
            fileDialogService: new FakeFileDialogService(null));

        viewModel.Configure(@"C:\data\sample.db", "users");
        viewModel.QueryText = "delete from users;";

        await viewModel.ExecuteQueryAsync();

        queryExecutor.LastSql.Should().BeNull();
        viewModel.HasResults.Should().BeFalse();
        viewModel.StatusMessage.Should().Contain("当前查询模式为只读");
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldPassAllowWrite_WhenWriteModeIsEnabled()
    {
        var queryExecutor = new FakeSqliteQueryExecutor(
            new QueryExecutionResult(
                Array.Empty<string>(),
                Array.Empty<IReadOnlyList<object?>>(),
                1,
                TimeSpan.FromMilliseconds(3),
                Message: "查询影响了 1 行。"));
        var viewModel = CreateViewModel(
            new QueryService(queryExecutor),
            exportService: new ExportService(new FakeCsvExportWriter()),
            fileDialogService: new FakeFileDialogService(null));

        viewModel.Configure(@"C:\data\sample.db", "users");
        viewModel.ToggleWriteMode();
        viewModel.QueryText = "delete from users where id = 1;";

        await viewModel.ExecuteQueryAsync();

        queryExecutor.LastSql.Should().Be("delete from users where id = 1;");
        queryExecutor.LastAllowWrite.Should().BeTrue();
        viewModel.QueryAccessModeLabel.Should().Be("可写模式");
    }

    [Fact]
    public void TemplateCommands_ShouldQuoteTableNamesWithEmbeddedQuotes()
    {
        var viewModel = CreateViewModel(
            new QueryService(new FakeSqliteQueryExecutor(
                new QueryExecutionResult(Array.Empty<string>(), Array.Empty<IReadOnlyList<object?>>(), 0, TimeSpan.Zero, string.Empty))),
            new ExportService(new FakeCsvExportWriter()),
            new FakeFileDialogService(null));

        viewModel.Configure(@"C:\data\sample.db", "weird\"table");

        viewModel.QueryText.Should().Be("select * from \"weird\"\"table\" limit 100;");
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldConfirmForDdlOperations()
    {
        var dialog = new TestDialogService(confirm: true);
        var queryExecutor = new FakeSqliteQueryExecutor(
            new QueryExecutionResult(
                Array.Empty<string>(),
                Array.Empty<IReadOnlyList<object?>>(),
                0,
                TimeSpan.FromMilliseconds(5),
                Message: "[DDL 变更] 影响了 0 行。"));
        var viewModel = CreateViewModel(
            new QueryService(queryExecutor),
            new ExportService(new FakeCsvExportWriter()),
            new FakeFileDialogService(null),
            dialog);

        viewModel.Configure(@"C:\data\sample.db");
        viewModel.ToggleWriteMode();
        viewModel.QueryText = "create table test(x integer);";

        await viewModel.ExecuteQueryAsync();

        dialog.ConfirmCallCount.Should().Be(1);
        queryExecutor.LastSql.Should().Be("create table test(x integer);");
        queryExecutor.LastAllowWrite.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldNotConfirmForDmlOperations()
    {
        var dialog = new TestDialogService(confirm: true);
        var queryExecutor = new FakeSqliteQueryExecutor(
            new QueryExecutionResult(
                Array.Empty<string>(),
                Array.Empty<IReadOnlyList<object?>>(),
                3,
                TimeSpan.FromMilliseconds(2),
                Message: "[DML 写入] 影响了 3 行。"));
        var viewModel = CreateViewModel(
            new QueryService(queryExecutor),
            new ExportService(new FakeCsvExportWriter()),
            new FakeFileDialogService(null),
            dialog);

        viewModel.Configure(@"C:\data\sample.db");
        viewModel.ToggleWriteMode();
        viewModel.QueryText = "update users set name = 'X';";

        await viewModel.ExecuteQueryAsync();

        dialog.ConfirmCallCount.Should().Be(0);
        queryExecutor.LastSql.Should().Be("update users set name = 'X';");
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldCancelExecution_WhenConfirmationDenied()
    {
        var dialog = new TestDialogService(confirm: false);
        var queryExecutor = new FakeSqliteQueryExecutor(
            new QueryExecutionResult(Array.Empty<string>(), Array.Empty<IReadOnlyList<object?>>(), 0, TimeSpan.Zero, "should not execute"));
        var viewModel = CreateViewModel(
            new QueryService(queryExecutor),
            new ExportService(new FakeCsvExportWriter()),
            new FakeFileDialogService(null),
            dialog);

        viewModel.Configure(@"C:\data\sample.db");
        viewModel.ToggleWriteMode();
        viewModel.QueryText = "drop table users;";

        await viewModel.ExecuteQueryAsync();

        dialog.ConfirmCallCount.Should().Be(1);
        queryExecutor.LastSql.Should().BeNull();
        viewModel.StatusMessage.Should().Be("已取消执行。");
    }

    [Fact]
    public void ToggleWriteMode_ShouldUpdateShowWriteWarning()
    {
        var viewModel = CreateViewModel(
            new QueryService(new FakeSqliteQueryExecutor(
                new QueryExecutionResult(Array.Empty<string>(), Array.Empty<IReadOnlyList<object?>>(), 0, TimeSpan.Zero, string.Empty))),
            new ExportService(new FakeCsvExportWriter()),
            new FakeFileDialogService(null));

        viewModel.ShowWriteWarning.Should().BeFalse();

        viewModel.ToggleWriteMode();

        viewModel.ShowWriteWarning.Should().BeTrue();
        viewModel.AllowWriteMode.Should().BeTrue();
    }

    private static QueryViewModel CreateViewModel(
        QueryService queryService,
        ExportService? exportService = null,
        IFileDialogService? fileDialogService = null,
        IDialogService? dialogService = null)
    {
        return new QueryViewModel(
            queryService,
            exportService ?? new ExportService(new FakeCsvExportWriter()),
            fileDialogService ?? new FakeFileDialogService(null),
            dialogService ?? new TestDialogService(confirm: true));
    }

    private sealed class TestDialogService : IDialogService
    {
        private readonly bool _confirm;

        public TestDialogService(bool confirm)
        {
            _confirm = confirm;
        }

        public int ConfirmCallCount { get; private set; }

        public bool Confirm(string title, string message)
        {
            ConfirmCallCount++;
            return _confirm;
        }
    }

    private sealed class FakeSqliteQueryExecutor : ISqliteQueryExecutor
    {
        private readonly QueryExecutionResult _result;

        public FakeSqliteQueryExecutor(QueryExecutionResult result)
        {
            _result = result;
        }

        public string? LastFilePath { get; private set; }

        public string? LastSql { get; private set; }
        public bool LastAllowWrite { get; private set; }

        public Task<QueryExecutionResult> ExecuteAsync(
            string filePath,
            string sql,
            bool allowWrite = false,
            CancellationToken cancellationToken = default)
        {
            LastFilePath = filePath;
            LastSql = sql;
            LastAllowWrite = allowWrite;
            return Task.FromResult(_result);
        }
    }

    private sealed class FakeCsvExportWriter : ICsvExportWriter
    {
        public string? LastFilePath { get; private set; }

        public IReadOnlyList<string> LastColumns { get; private set; } = Array.Empty<string>();

        public IReadOnlyList<IReadOnlyList<object?>> LastRows { get; private set; } = Array.Empty<IReadOnlyList<object?>>();

        public Task WriteAsync(
            string filePath,
            IReadOnlyList<string> columns,
            IReadOnlyList<IReadOnlyList<object?>> rows,
            CancellationToken cancellationToken = default)
        {
            LastFilePath = filePath;
            LastColumns = columns.ToArray();
            LastRows = rows.Select(row => (IReadOnlyList<object?>)row.ToArray()).ToArray();
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingSqliteQueryExecutor : ISqliteQueryExecutor
    {
        private readonly string _message;

        public ThrowingSqliteQueryExecutor(string message)
        {
            _message = message;
        }

        public Task<QueryExecutionResult> ExecuteAsync(
            string filePath,
            string sql,
            bool allowWrite = false,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(_message);
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        private readonly string? _csvPath;

        public FakeFileDialogService(string? csvPath)
        {
            _csvPath = csvPath;
        }

        public string? PickSqliteFile() => null;

        public string? PickCsvSavePath(string suggestedFileName) => _csvPath;
    }
}
