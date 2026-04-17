using FluentAssertions;
using OpenDbViewer.Infrastructure.Sqlite.Sqlite;
using OpenDbViewer.Shell.ViewModels;
using OpenDbViewer.Wpf.Tests.Support;

namespace OpenDbViewer.Wpf.Tests.ViewModels;

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
}
