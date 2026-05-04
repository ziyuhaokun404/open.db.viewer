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
        var viewModel = new QueryViewModel(
            new QueryService(queryExecutor),
            new ExportService(new FakeCsvExportWriter()),
            new FakeFileDialogService(@"C:\exports\result.csv"));

        viewModel.Configure(@"C:\data\sample.db", "users");

        await viewModel.ExecuteQueryAsync();

        queryExecutor.LastFilePath.Should().Be(@"C:\data\sample.db");
        queryExecutor.LastSql.Should().Be("select * from \"users\" limit 100;");
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
        var viewModel = new QueryViewModel(
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
        var viewModel = new QueryViewModel(
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
        var viewModel = new QueryViewModel(
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
        var viewModel = new QueryViewModel(
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

    private sealed class FakeSqliteQueryExecutor : ISqliteQueryExecutor
    {
        private readonly QueryExecutionResult _result;

        public FakeSqliteQueryExecutor(QueryExecutionResult result)
        {
            _result = result;
        }

        public string? LastFilePath { get; private set; }

        public string? LastSql { get; private set; }

        public Task<QueryExecutionResult> ExecuteAsync(string filePath, string sql, CancellationToken cancellationToken = default)
        {
            LastFilePath = filePath;
            LastSql = sql;
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

        public Task<QueryExecutionResult> ExecuteAsync(string filePath, string sql, CancellationToken cancellationToken = default) =>
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
