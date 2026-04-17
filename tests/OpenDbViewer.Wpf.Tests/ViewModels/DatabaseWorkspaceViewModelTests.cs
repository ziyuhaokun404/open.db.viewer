using FluentAssertions;
using OpenDbViewer.Infrastructure.Sqlite.Sqlite;
using OpenDbViewer.Shell.ViewModels;
using OpenDbViewer.Wpf.Tests.Support;

namespace OpenDbViewer.Wpf.Tests.ViewModels;

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
        var viewModel = new DatabaseWorkspaceViewModel(objectExplorer, schema, data);

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
            new DataViewModel(new SqliteTableDataReader(connectionFactory)));

        await viewModel.LoadAsync(db.FilePath);

        var usersNode = viewModel.ObjectExplorer.RootNodes
            .SelectMany(root => root.Children ?? Array.Empty<OpenDbViewer.Domain.Models.DatabaseObjectNode>())
            .Single(node => node.Name == "users");

        await viewModel.SelectNodeAsync(usersNode);

        viewModel.ObjectExplorer.SelectedNode.Should().Be(usersNode);
        viewModel.Schema.TableName.Should().Be("users");
        viewModel.Schema.Columns.Select(column => column.Name).Should().Equal("id", "name", "email");
        viewModel.Data.Columns.Should().Equal("id", "name", "email");
        viewModel.Data.Rows.Should().HaveCount(3);
        viewModel.Data.Rows[0].Values.Should().Equal(1, "Alice", "alice@example.com");
    }
}
