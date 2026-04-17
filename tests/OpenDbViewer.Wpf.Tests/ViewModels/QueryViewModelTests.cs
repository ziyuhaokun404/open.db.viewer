using FluentAssertions;
using OpenDbViewer.Application.Abstractions;
using OpenDbViewer.Application.Services;
using OpenDbViewer.Domain.Models;
using OpenDbViewer.Shell.Services;
using OpenDbViewer.Shell.ViewModels;

namespace OpenDbViewer.Wpf.Tests.ViewModels;

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
                Message: "Query returned 2 row(s)."));
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
        viewModel.StatusMessage.Should().Contain("Query returned 2 row(s).");
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
                    Message: "Query returned 1 row(s)."))),
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
