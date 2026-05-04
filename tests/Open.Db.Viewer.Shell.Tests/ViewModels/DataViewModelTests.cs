using FluentAssertions;

using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;
using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.Shell.Tests.Support;
using Open.Db.Viewer.ShellHost.Services;

namespace Open.Db.Viewer.Shell.Tests.ViewModels;

public class DataViewModelTests
{
    [Fact]
    public async Task LoadFirstPageAsync_ShouldPopulateFirstPageAndEnableNextPage()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var viewModel = new DataViewModel(new SqliteTableDataReader(connectionFactory))
        {
            PageSize = 2
        };

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");

        viewModel.PageNumber.Should().Be(1);
        viewModel.HasPreviousPage.Should().BeFalse();
        viewModel.HasNextPage.Should().BeTrue();
        viewModel.Rows.Should().HaveCount(2);
        viewModel.Rows[0].Values.Should().Equal(1, "Alice", "alice@example.com");
    }

    [Fact]
    public async Task LoadNextPageAsync_ShouldAdvanceAndLoadRemainingRows()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var viewModel = new DataViewModel(new SqliteTableDataReader(connectionFactory))
        {
            PageSize = 2
        };

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");
        await viewModel.LoadNextPageAsync();

        viewModel.PageNumber.Should().Be(2);
        viewModel.HasPreviousPage.Should().BeTrue();
        viewModel.HasNextPage.Should().BeFalse();
        viewModel.Rows.Should().ContainSingle();
        viewModel.Rows[0].Values[0].Should().Be(3L);
        viewModel.Rows[0].Values[1].Should().Be("Charlie");
        (viewModel.Rows[0].Values[2] is null || viewModel.Rows[0].Values[2] is DBNull).Should().BeTrue();
    }

    [Fact]
    public async Task LoadPreviousPageAsync_ShouldReturnToFirstPage()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var viewModel = new DataViewModel(new SqliteTableDataReader(connectionFactory))
        {
            PageSize = 2
        };

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");
        await viewModel.LoadNextPageAsync();
        await viewModel.LoadPreviousPageAsync();

        viewModel.PageNumber.Should().Be(1);
        viewModel.HasPreviousPage.Should().BeFalse();
        viewModel.HasNextPage.Should().BeTrue();
        viewModel.Rows.Should().HaveCount(2);
        viewModel.Rows[0].Values.Should().Equal(1, "Alice", "alice@example.com");
    }

    [Fact]
    public async Task ChangePageSizeAsync_ShouldResetToFirstPageAndRefreshRows()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var viewModel = new DataViewModel(new SqliteTableDataReader(connectionFactory))
        {
            PageSize = 1
        };

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");
        await viewModel.LoadNextPageAsync();

        await viewModel.ChangePageSizeAsync(2);

        viewModel.PageSize.Should().Be(2);
        viewModel.PageNumber.Should().Be(1);
        viewModel.HasNextPage.Should().BeTrue();
        viewModel.Rows.Should().HaveCount(2);
        viewModel.Rows[0].Values.Should().Equal(1, "Alice", "alice@example.com");
    }

    [Fact]
    public async Task ApplySortAsync_ShouldSortBySelectedColumnAndToggleDirection()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var viewModel = new DataViewModel(new SqliteTableDataReader(connectionFactory))
        {
            PageSize = 3
        };

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");

        await viewModel.ApplySortAsync("name");

        viewModel.SortColumn.Should().Be("name");
        viewModel.SortDirection.Should().Be("ASC");
        viewModel.Rows.Select(row => row.Values[1]).Should().Equal("Alice", "Bob", "Charlie");

        await viewModel.ApplySortAsync("name");

        viewModel.SortColumn.Should().Be("name");
        viewModel.SortDirection.Should().Be("DESC");
        viewModel.Rows.Select(row => row.Values[1]).Should().Equal("Charlie", "Bob", "Alice");
    }

    [Fact]
    public async Task ExportCurrentPageAsync_ShouldWriteVisibleRowsToCsv()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var exportWriter = new RecordingCsvExportWriter();
        var viewModel = new DataViewModel(
            new SqliteTableDataReader(new SqliteConnectionFactory()),
            new ExportService(exportWriter),
            new FakeFileDialogService(@"C:\exports\users-page.csv"))
        {
            PageSize = 2
        };

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");
        await viewModel.ExportCurrentPageAsync();

        exportWriter.FilePath.Should().Be(@"C:\exports\users-page.csv");
        exportWriter.Rows.Should().HaveCount(2);
        viewModel.StatusMessage.Should().Contain("users-page.csv");
    }

    [Fact]
    public async Task LoadFirstPageAsync_ShouldExposeResultSummary()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var viewModel = new DataViewModel(new SqliteTableDataReader(new SqliteConnectionFactory()));

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");

        viewModel.HasRows.Should().BeTrue();
        viewModel.ResultSummary.Should().Be("第 1 页 · 3 列 · 当前显示 3 行");
    }

    private sealed class RecordingCsvExportWriter : ICsvExportWriter
    {
        public string? FilePath { get; private set; }

        public IReadOnlyList<IReadOnlyList<object?>> Rows { get; private set; } = Array.Empty<IReadOnlyList<object?>>();

        public Task WriteAsync(string filePath, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<object?>> rows, CancellationToken cancellationToken = default)
        {
            FilePath = filePath;
            Rows = rows.Select(row => (IReadOnlyList<object?>)row.ToArray()).ToArray();
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
