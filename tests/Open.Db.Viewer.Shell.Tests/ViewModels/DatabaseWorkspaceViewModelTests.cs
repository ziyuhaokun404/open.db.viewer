using FluentAssertions;
using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;
using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.Shell.Tests.Support;

namespace Open.Db.Viewer.Shell.Tests.ViewModels;

public class DatabaseWorkspaceViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShouldPopulateObjectTreeAndSelectFirstTable()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var objectExplorer = new ObjectExplorerViewModel(new SqliteDatabaseInspector(connectionFactory));
        var schema = new SchemaViewModel(new SqliteDatabaseInspector(connectionFactory));
        var data = new DataViewModel(new SqliteTableDataReader(connectionFactory));
        var query = new QueryViewModel(
            new Open.Db.Viewer.Application.Services.QueryService(new SqliteQueryExecutor(connectionFactory)),
            new Open.Db.Viewer.Application.Services.ExportService(new Open.Db.Viewer.Infrastructure.Sqlite.Export.CsvExportWriter()),
            new FakeFileDialogService());
        var viewModel = new DatabaseWorkspaceViewModel(objectExplorer, schema, data, query);

        await viewModel.LoadAsync(db.FilePath);

        viewModel.DatabasePath.Should().Be(db.FilePath);
        viewModel.Title.Should().Be("sample");
        viewModel.ObjectExplorer.RootNodes.Should().ContainSingle();
        viewModel.ObjectExplorer.SelectedNode.Should().NotBeNull();
        viewModel.ObjectExplorer.SelectedNode!.Name.Should().Be("orders");
        viewModel.Schema.TableName.Should().Be("orders");
        viewModel.Schema.Columns.Should().HaveCount(3);
        viewModel.Data.Columns.Should().Equal("id", "user_id", "total");
        viewModel.Data.Rows.Should().HaveCount(2);
        viewModel.Data.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task SelectNodeAsync_ShouldLoadSchemaAndFirstPageForSelectedTable()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var inspector = new SqliteDatabaseInspector(connectionFactory);
        var viewModel = new DatabaseWorkspaceViewModel(
            new ObjectExplorerViewModel(inspector),
            new SchemaViewModel(inspector),
            new DataViewModel(new SqliteTableDataReader(connectionFactory)),
            new QueryViewModel(
                new Open.Db.Viewer.Application.Services.QueryService(new SqliteQueryExecutor(connectionFactory)),
                new Open.Db.Viewer.Application.Services.ExportService(new Open.Db.Viewer.Infrastructure.Sqlite.Export.CsvExportWriter()),
                new FakeFileDialogService()));

        await viewModel.LoadAsync(db.FilePath);

        var usersNode = viewModel.ObjectExplorer.RootNodes
            .SelectMany(root => root.Children ?? Array.Empty<Open.Db.Viewer.Domain.Models.DatabaseObjectNode>())
            .Single(node => node.Name == "users");

        await viewModel.SelectNodeAsync(usersNode);

        viewModel.ObjectExplorer.SelectedNode.Should().Be(usersNode);
        viewModel.Schema.TableName.Should().Be("users");
        viewModel.Schema.Columns.Select(column => column.Name).Should().Equal("id", "name", "email");
        viewModel.Data.Columns.Should().Equal("id", "name", "email");
        viewModel.Data.Rows.Should().HaveCount(3);
        viewModel.Data.Rows[0].Values.Should().Equal(1, "Alice", "alice@example.com");
    }

    [Fact]
    public async Task SelectNodeAsync_ShouldResetDataPageNumberWhenSwitchingTables()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var inspector = new SqliteDatabaseInspector(connectionFactory);
        var data = new DataViewModel(new SqliteTableDataReader(connectionFactory))
        {
            PageSize = 1
        };
        var viewModel = new DatabaseWorkspaceViewModel(
            new ObjectExplorerViewModel(inspector),
            new SchemaViewModel(inspector),
            data,
            new QueryViewModel(
                new Open.Db.Viewer.Application.Services.QueryService(new SqliteQueryExecutor(connectionFactory)),
                new Open.Db.Viewer.Application.Services.ExportService(new Open.Db.Viewer.Infrastructure.Sqlite.Export.CsvExportWriter()),
                new FakeFileDialogService()));

        await viewModel.LoadAsync(db.FilePath);
        await viewModel.Data.LoadNextPageAsync();

        var usersNode = viewModel.ObjectExplorer.RootNodes
            .SelectMany(root => root.Children ?? Array.Empty<Open.Db.Viewer.Domain.Models.DatabaseObjectNode>())
            .Single(node => node.Name == "users");

        await viewModel.SelectNodeAsync(usersNode);

        viewModel.Data.PageNumber.Should().Be(1);
        viewModel.Data.Rows.Should().ContainSingle();
        viewModel.Data.Rows[0].Values.Should().Equal(1, "Alice", "alice@example.com");
    }

    [Fact]
    public async Task RefreshAsync_ShouldReloadCurrentSelection_AndUpdateStatus()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var inspector = new SqliteDatabaseInspector(connectionFactory);
        var viewModel = new DatabaseWorkspaceViewModel(
            new ObjectExplorerViewModel(inspector),
            new SchemaViewModel(inspector),
            new DataViewModel(new SqliteTableDataReader(connectionFactory)),
            new QueryViewModel(
                new QueryService(new SqliteQueryExecutor(connectionFactory)),
                new ExportService(new Open.Db.Viewer.Infrastructure.Sqlite.Export.CsvExportWriter()),
                new FakeFileDialogService()));

        await viewModel.LoadAsync(db.FilePath);
        var originalSelection = viewModel.ObjectExplorer.SelectedNode;

        await viewModel.RefreshAsync();

        viewModel.ObjectExplorer.SelectedNode?.Name.Should().Be(originalSelection?.Name);
        viewModel.StatusMessage.Should().Be("工作区已刷新。");
    }

    [Fact]
    public void ClearState_ShouldExposeNoSelectionWorkspaceState()
    {
        var viewModel = new DatabaseWorkspaceViewModel(
            new ObjectExplorerViewModel(),
            new SchemaViewModel(),
            new DataViewModel(),
            new QueryViewModel(
                new QueryService(new NoopSqliteQueryExecutor()),
                new ExportService(new NoopCsvExportWriter()),
                new FakeFileDialogService()));

        viewModel.ObjectExplorer.SelectedNode = null;
        viewModel.Schema.Clear();
        viewModel.Data.Clear();

        viewModel.HasTableSelection.Should().BeFalse();
        viewModel.SelectedObjectTitle.Should().Be("未选择数据表");
    }

    [Fact]
    public void SchemaViewModel_ShouldExposeColumnCountSummary()
    {
        var viewModel = new SchemaViewModel();
        viewModel.Columns.Add(new TableColumnInfo("id", "INTEGER", false, null, true));
        viewModel.Columns.Add(new TableColumnInfo("name", "TEXT", true, null, false));

        viewModel.ColumnCountSummary.Should().Be("2 列");
    }

    [Fact]
    public void Workspace_ShouldExposeEmptyState_WhenNoDatabaseIsOpen()
    {
        var viewModel = new DatabaseWorkspaceViewModel(
            new ObjectExplorerViewModel(),
            new SchemaViewModel(),
            new DataViewModel(),
            new QueryViewModel(
                new QueryService(new NoopSqliteQueryExecutor()),
                new ExportService(new NoopCsvExportWriter()),
                new FakeFileDialogService()));

        viewModel.HasOpenDatabase.Should().BeFalse();
        viewModel.EmptyStateTitle.Should().Be("尚未打开数据库");
        viewModel.EmptyStateDescription.Should().Contain("打开一个 SQLite 数据库");
    }

    private sealed class FakeFileDialogService : Open.Db.Viewer.Shell.Services.IFileDialogService
    {
        public string? PickSqliteFile() => null;

        public string? PickCsvSavePath(string suggestedFileName) => null;
    }

    private sealed class NoopSqliteQueryExecutor : ISqliteQueryExecutor
    {
        public Task<QueryExecutionResult> ExecuteAsync(string filePath, string sql, CancellationToken cancellationToken = default) =>
            Task.FromResult(new QueryExecutionResult(Array.Empty<string>(), Array.Empty<IReadOnlyList<object?>>(), 0, TimeSpan.Zero, string.Empty));
    }

    private sealed class NoopCsvExportWriter : ICsvExportWriter
    {
        public Task WriteAsync(string filePath, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<object?>> rows, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
