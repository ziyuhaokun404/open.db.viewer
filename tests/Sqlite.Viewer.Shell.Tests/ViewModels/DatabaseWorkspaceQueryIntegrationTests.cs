using FluentAssertions;
using Sqlite.Viewer.Infrastructure.Sqlite.Sqlite;
using Sqlite.Viewer.Shell.Tests.Support;
using Sqlite.Viewer.Shell.ViewModels;
using Sqlite.Viewer.ShellHost.Services;

namespace Sqlite.Viewer.Shell.Tests.ViewModels;

public class DatabaseWorkspaceQueryIntegrationTests
{
    [Fact]
    public async Task SelectNodeAsync_ShouldUpdateQueryContextForSelectedTable()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var inspector = new SqliteDatabaseInspector(connectionFactory);
        var queryViewModel = new QueryViewModel(
            new Application.Services.QueryService(new SqliteQueryExecutor(connectionFactory)),
            new Application.Services.ExportService(new Infrastructure.Sqlite.Export.CsvExportWriter()),
            new FakeFileDialogService(),
            new Support.NoopDialogService(), new Support.InMemoryAppSettingsStore(), new Support.InMemoryQueryHistoryStore());
        var viewModel = new DatabaseWorkspaceViewModel(
            new ObjectExplorerViewModel(inspector),
            new SchemaViewModel(inspector),
            new DataViewModel(new SqliteTableDataReader(connectionFactory)),
            queryViewModel);

        await viewModel.LoadAsync(db.FilePath);

        var usersNode = viewModel.ObjectExplorer.RootNodes
            .SelectMany(root => root.Children ?? Array.Empty<Domain.Models.DatabaseObjectNode>())
            .Single(node => node.Name == "users");

        await viewModel.SelectNodeAsync(usersNode);

        viewModel.Query.DatabasePath.Should().Be(db.FilePath);
        viewModel.Query.QueryText.Should().Be("select * from \"users\" limit 100;");
        viewModel.Query.StatusMessage.Should().Be(QueryViewModel.ReadyStatusMessage);
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string? PickSqliteFile() => null;

        public string? PickCsvSavePath(string suggestedFileName) => null;
    }
}
