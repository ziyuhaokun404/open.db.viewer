using FluentAssertions;
using Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;
using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.Shell.Tests.Support;

namespace Open.Db.Viewer.Shell.Tests.ViewModels;

public class DatabaseWorkspaceQueryIntegrationTests
{
    [Fact]
    public async Task SelectNodeAsync_ShouldUpdateQueryContextForSelectedTable()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var inspector = new SqliteDatabaseInspector(connectionFactory);
        var queryViewModel = new QueryViewModel(
            new Open.Db.Viewer.Application.Services.QueryService(new SqliteQueryExecutor(connectionFactory)),
            new Open.Db.Viewer.Application.Services.ExportService(new Open.Db.Viewer.Infrastructure.Sqlite.Export.CsvExportWriter()),
            new FakeFileDialogService());
        var viewModel = new DatabaseWorkspaceViewModel(
            new ObjectExplorerViewModel(inspector),
            new SchemaViewModel(inspector),
            new DataViewModel(new SqliteTableDataReader(connectionFactory)),
            queryViewModel);

        await viewModel.LoadAsync(db.FilePath);

        var usersNode = viewModel.ObjectExplorer.RootNodes
            .SelectMany(root => root.Children ?? Array.Empty<Open.Db.Viewer.Domain.Models.DatabaseObjectNode>())
            .Single(node => node.Name == "users");

        await viewModel.SelectNodeAsync(usersNode);

        viewModel.Query.DatabasePath.Should().Be(db.FilePath);
        viewModel.Query.QueryText.Should().Be("select * from \"users\" limit 100;");
        viewModel.Query.StatusMessage.Should().Be(QueryViewModel.ReadyStatusMessage);
    }

    private sealed class FakeFileDialogService : Open.Db.Viewer.Shell.Services.IFileDialogService
    {
        public string? PickSqliteFile() => null;

        public string? PickCsvSavePath(string suggestedFileName) => null;
    }
}
