using FluentAssertions;
using Sqlite.Viewer.Application.Abstractions;
using Sqlite.Viewer.Application.Services;
using Sqlite.Viewer.Domain.Models;
using Sqlite.Viewer.Infrastructure.Sqlite.Sqlite;
using Sqlite.Viewer.Shell.Tests.Support;
using Sqlite.Viewer.Shell.ViewModels;
using Sqlite.Viewer.ShellHost.Services;

namespace Sqlite.Viewer.Shell.Tests.ViewModels;

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
            new QueryService(new SqliteQueryExecutor(connectionFactory)),
            new ExportService(new Infrastructure.Sqlite.Export.CsvExportWriter()),
            new FakeFileDialogService(),
            new Support.NoopDialogService(), new Support.InMemoryAppSettingsStore(), new Support.InMemoryQueryHistoryStore());
        var viewModel = new DatabaseWorkspaceViewModel(objectExplorer, schema, data, query);

        await viewModel.LoadAsync(db.FilePath);

        viewModel.DatabasePath.Should().Be(db.FilePath);
        viewModel.Title.Should().Be("sample");
        viewModel.ObjectExplorer.RootNodes.Should().HaveCount(5);
        viewModel.ObjectExplorer.RootNodes.Select(n => n.Id).Should().Equal(
            "group:tables", "group:views", "group:indexes", "group:triggers", "group:system");
        viewModel.ObjectExplorer.SelectedNode.Should().NotBeNull();
        viewModel.ObjectExplorer.SelectedNode!.Name.Should().Be("orders");
        viewModel.ObjectExplorer.FilteredTables.Should().Contain(node => node.Kind == "view" && node.Name == "user_names");
        viewModel.Schema.TableName.Should().Be("orders");
        viewModel.Schema.Columns.Should().HaveCount(3);
        viewModel.Schema.RowCount.Should().Be(2);
        viewModel.Schema.Indexes.Should().ContainSingle(index => index.Name == "idx_orders_user_id");
        viewModel.Schema.SqliteVersion.Should().NotBeNullOrWhiteSpace();
        viewModel.Schema.EncodingSummary.Should().NotBe("-");
        viewModel.Data.Columns.Should().Equal("id", "user_id", "total");
        viewModel.Data.Rows.Should().HaveCount(2);
        viewModel.Data.PageNumber.Should().Be(1);
        viewModel.DatabaseFileName.Should().Be("sample.db");
        viewModel.ConnectionStatusText.Should().Be("连接正常");
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
                new QueryService(new SqliteQueryExecutor(connectionFactory)),
                new ExportService(new Infrastructure.Sqlite.Export.CsvExportWriter()),
                new FakeFileDialogService(),
                new Support.NoopDialogService(), new Support.InMemoryAppSettingsStore(), new Support.InMemoryQueryHistoryStore()));

        await viewModel.LoadAsync(db.FilePath);

        var usersNode = viewModel.ObjectExplorer.RootNodes
            .SelectMany(root => root.Children ?? Array.Empty<DatabaseObjectNode>())
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
                new QueryService(new SqliteQueryExecutor(connectionFactory)),
                new ExportService(new Infrastructure.Sqlite.Export.CsvExportWriter()),
                new FakeFileDialogService(),
                new Support.NoopDialogService(), new Support.InMemoryAppSettingsStore(), new Support.InMemoryQueryHistoryStore()));

        await viewModel.LoadAsync(db.FilePath);
        await viewModel.Data.LoadNextPageAsync();

        var usersNode = viewModel.ObjectExplorer.RootNodes
            .SelectMany(root => root.Children ?? Array.Empty<DatabaseObjectNode>())
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
                new ExportService(new Infrastructure.Sqlite.Export.CsvExportWriter()),
                new FakeFileDialogService(),
                new Support.NoopDialogService(), new Support.InMemoryAppSettingsStore(), new Support.InMemoryQueryHistoryStore()));

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
                new FakeFileDialogService(),
                new Support.NoopDialogService(), new Support.InMemoryAppSettingsStore(), new Support.InMemoryQueryHistoryStore()));

        viewModel.ObjectExplorer.SelectedNode = null;
        viewModel.Schema.Clear();
        viewModel.Data.Clear();

        viewModel.HasTableSelection.Should().BeFalse();
        viewModel.SelectedObjectTitle.Should().Be("未选择对象");
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
                new FakeFileDialogService(),
                new Support.NoopDialogService(), new Support.InMemoryAppSettingsStore(), new Support.InMemoryQueryHistoryStore()));

        viewModel.HasOpenDatabase.Should().BeFalse();
        viewModel.EmptyStateTitle.Should().Be("尚未打开数据库");
        viewModel.EmptyStateDescription.Should().Contain("打开一个 SQLite 数据库");
    }

    [Fact]
    public async Task ObjectExplorer_ShouldFilterTablesBySearchText()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var viewModel = new ObjectExplorerViewModel(new SqliteDatabaseInspector(connectionFactory));

        await viewModel.LoadAsync(db.FilePath);
        viewModel.SearchText = "user_names";

        viewModel.FilteredTables.Should().ContainSingle(node => node.Name == "user_names");
        viewModel.ObjectCountSummary.Should().Be("显示 1 / 5 个对象");
        viewModel.SelectedNode?.Name.Should().Be("user_names");
    }

    [Fact]
    public async Task SelectNodeAsync_ShouldLoadDdlForIndex()
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
                new ExportService(new Infrastructure.Sqlite.Export.CsvExportWriter()),
                new FakeFileDialogService(),
                new Support.NoopDialogService(), new Support.InMemoryAppSettingsStore(), new Support.InMemoryQueryHistoryStore()));

        await viewModel.LoadAsync(db.FilePath);

        var indexNode = viewModel.ObjectExplorer.FilteredTables
            .Single(node => node.Kind == "index" && node.Name == "idx_orders_user_id");

        await viewModel.SelectNodeAsync(indexNode);

        viewModel.Schema.TableName.Should().Be("idx_orders_user_id");
        viewModel.Schema.CreateSql.Should().Contain("CREATE INDEX");
        viewModel.Schema.HasColumns.Should().BeFalse();
        viewModel.Data.Rows.Should().BeEmpty();
        viewModel.HasTableSelection.Should().BeFalse();
        viewModel.HasObjectSelection.Should().BeTrue();
    }

    [Fact]
    public async Task SelectNodeAsync_ShouldLoadViewSchemaAndData()
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
                new ExportService(new Infrastructure.Sqlite.Export.CsvExportWriter()),
                new FakeFileDialogService(),
                new Support.NoopDialogService(), new Support.InMemoryAppSettingsStore(), new Support.InMemoryQueryHistoryStore()));

        await viewModel.LoadAsync(db.FilePath);

        var viewNode = viewModel.ObjectExplorer.FilteredTables
            .Single(node => node.Kind == "view" && node.Name == "user_names");

        await viewModel.SelectNodeAsync(viewNode);

        viewModel.Schema.TableName.Should().Be("user_names");
        viewModel.Schema.CreateSql.Should().Contain("CREATE VIEW");
        viewModel.Schema.Columns.Select(c => c.Name).Should().Equal("id", "name");
        viewModel.Data.Rows.Should().HaveCount(3);
        viewModel.HasTableSelection.Should().BeTrue();
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string? PickSqliteFile() => null;

        public string? PickCsvSavePath(string suggestedFileName) => null;
    }

    private sealed class NoopSqliteQueryExecutor : ISqliteQueryExecutor
    {
        public Task<QueryExecutionResult> ExecuteAsync(
            string filePath,
            string sql,
            bool allowWrite = false,
            int? maxResultRows = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new QueryExecutionResult(Array.Empty<string>(), Array.Empty<IReadOnlyList<object?>>(), 0, TimeSpan.Zero, string.Empty));
    }

    private sealed class NoopCsvExportWriter : ICsvExportWriter
    {
        public Task WriteAsync(string filePath, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<object?>> rows, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task WriteStreamingAsync(
            string filePath,
            IReadOnlyList<string> columns,
            IAsyncEnumerable<IReadOnlyList<object?>> rows,
            IProgress<long>? rowsWrittenProgress = null,
            CancellationToken cancellationToken = default)
        {
            await foreach (var _ in rows.WithCancellation(cancellationToken))
            {
            }
        }
    }
}
