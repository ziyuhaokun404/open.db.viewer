using FluentAssertions;
using Sqlite.Viewer.Application.Abstractions;
using Sqlite.Viewer.Application.Services;
using Sqlite.Viewer.Domain.Models;
using Sqlite.Viewer.Infrastructure.Sqlite.Sqlite;
using Sqlite.Viewer.Shell.Tests.Support;
using Sqlite.Viewer.Shell.ViewModels;
using Sqlite.Viewer.ShellHost.Services;

namespace Sqlite.Viewer.Shell.Tests.ViewModels;

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
        viewModel.StatusMessage.Should().Contain("仅当前页");
    }

    [Fact]
    public async Task LoadFirstPageAsync_ShouldExposeResultSummary()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var viewModel = new DataViewModel(new SqliteTableDataReader(new SqliteConnectionFactory()));

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");

        viewModel.HasRows.Should().BeTrue();
        viewModel.TotalRowCount.Should().Be(3);
        viewModel.TotalPages.Should().Be(1);
        viewModel.ResultSummary.Should().Be("第 1/1 页 · 1–3 / 共 3 行 · 3 列");
    }

    [Fact]
    public async Task GoToPageAsync_ShouldJumpToTargetPage()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var viewModel = new DataViewModel(new SqliteTableDataReader(new SqliteConnectionFactory()))
        {
            PageSize = 1
        };

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");
        viewModel.JumpToPageNumber = 3;
        await viewModel.GoToPageAsync();

        viewModel.PageNumber.Should().Be(3);
        viewModel.Rows.Should().ContainSingle();
        viewModel.Rows[0].Values[1].Should().Be("Charlie");
    }

    [Fact]
    public async Task ApplyFilterAsync_ShouldFilterContainsAndReportTotal()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var viewModel = new DataViewModel(new SqliteTableDataReader(new SqliteConnectionFactory()))
        {
            PageSize = 10
        };

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");
        viewModel.FilterColumn = "name";
        viewModel.FilterOperator = TableFilterOperator.Contains;
        viewModel.FilterValue = "li";
        await viewModel.ApplyFilterAsync();

        viewModel.HasActiveFilter.Should().BeTrue();
        viewModel.TotalRowCount.Should().Be(2); // Alice, Charlie
        viewModel.Rows.Select(r => r.Values[1]).Should().Equal("Alice", "Charlie");
        viewModel.ResultSummary.Should().Contain("已筛选");
    }

    [Fact]
    public async Task ApplyFilterAsync_IsNull_ShouldMatchNullEmails()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var viewModel = new DataViewModel(new SqliteTableDataReader(new SqliteConnectionFactory()));

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");
        viewModel.FilterColumn = "email";
        viewModel.FilterOperator = TableFilterOperator.IsNull;
        await viewModel.ApplyFilterAsync();

        viewModel.TotalRowCount.Should().Be(1);
        viewModel.Rows.Should().ContainSingle();
        viewModel.Rows[0].Values[1].Should().Be("Charlie");
        // Display grid uses (NULL) marker while raw values stay null.
        viewModel.TableView.Should().NotBeNull();
        viewModel.TableView![0]["email"].Should().Be("(NULL)");
    }

    [Fact]
    public async Task ClearFilterAsync_ShouldRestoreAllRows()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var viewModel = new DataViewModel(new SqliteTableDataReader(new SqliteConnectionFactory()));

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");
        viewModel.FilterColumn = "name";
        viewModel.FilterOperator = TableFilterOperator.Equals;
        viewModel.FilterValue = "Bob";
        await viewModel.ApplyFilterAsync();
        await viewModel.ClearFilterAsync();

        viewModel.HasActiveFilter.Should().BeFalse();
        viewModel.TotalRowCount.Should().Be(3);
        viewModel.Rows.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExportFullTableAsync_ShouldStreamAllRows()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var exportWriter = new RecordingCsvExportWriter();
        var viewModel = new DataViewModel(
            new SqliteTableDataReader(new SqliteConnectionFactory()),
            new ExportService(exportWriter),
            new FakeFileDialogService(@"C:\exports\users-full.csv"))
        {
            PageSize = 1
        };

        await viewModel.LoadFirstPageAsync(db.FilePath, "users");
        await viewModel.ExportFullTableAsync();

        exportWriter.FilePath.Should().Be(@"C:\exports\users-full.csv");
        exportWriter.Rows.Should().HaveCount(3);
        viewModel.StatusMessage.Should().Contain("users-full.csv");
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

        public async Task WriteStreamingAsync(
            string filePath,
            IReadOnlyList<string> columns,
            IAsyncEnumerable<IReadOnlyList<object?>> rows,
            IProgress<long>? rowsWrittenProgress = null,
            CancellationToken cancellationToken = default)
        {
            var buffer = new List<IReadOnlyList<object?>>();
            await foreach (var row in rows.WithCancellation(cancellationToken))
            {
                buffer.Add(row.ToArray());
                rowsWrittenProgress?.Report(buffer.Count);
            }

            await WriteAsync(filePath, columns, buffer, cancellationToken);
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
