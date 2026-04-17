using FluentAssertions;
using OpenDbViewer.Infrastructure.Sqlite.Sqlite;
using OpenDbViewer.Shell.ViewModels;
using OpenDbViewer.Wpf.Tests.Support;

namespace OpenDbViewer.Wpf.Tests.ViewModels;

public class DatabaseWorkspaceQueryIntegrationTests
{
    [Fact]
    public async Task SelectNodeAsync_ShouldUpdateQueryContextForSelectedTable()
    {
        await using var db = await SqliteTestDb.CreateAsync();
        var connectionFactory = new SqliteConnectionFactory();
        var inspector = new SqliteDatabaseInspector(connectionFactory);
        var queryViewModel = new QueryViewModel(
            new OpenDbViewer.Application.Services.QueryService(new SqliteQueryExecutor(connectionFactory)),
            new OpenDbViewer.Application.Services.ExportService(new OpenDbViewer.Infrastructure.Sqlite.Export.CsvExportWriter()),
            new FakeFileDialogService());
        var viewModel = new DatabaseWorkspaceViewModel(
            new ObjectExplorerViewModel(inspector),
            new SchemaViewModel(inspector),
            new DataViewModel(new SqliteTableDataReader(connectionFactory)),
            queryViewModel);

        await viewModel.LoadAsync(db.FilePath);

        var usersNode = viewModel.ObjectExplorer.RootNodes
            .SelectMany(root => root.Children ?? Array.Empty<OpenDbViewer.Domain.Models.DatabaseObjectNode>())
            .Single(node => node.Name == "users");

        await viewModel.SelectNodeAsync(usersNode);

        viewModel.Query.DatabasePath.Should().Be(db.FilePath);
        viewModel.Query.QueryText.Should().Be("select * from \"users\" limit 100;");
        viewModel.Query.StatusMessage.Should().Be(QueryViewModel.ReadyStatusMessage);
    }

    private sealed class FakeFileDialogService : OpenDbViewer.Shell.Services.IFileDialogService
    {
        public string? PickSqliteFile() => null;

        public string? PickCsvSavePath(string suggestedFileName) => null;
    }
}
