using CommunityToolkit.Mvvm.ComponentModel;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;
using System.Collections.ObjectModel;

namespace Open.Db.Viewer.Shell.ViewModels;

public partial class ObjectExplorerViewModel : ObservableObject
{
    private readonly SqliteDatabaseInspector? _databaseInspector;

    [ObservableProperty]
    private DatabaseObjectNode? selectedNode;

    public ObjectExplorerViewModel()
    {
    }

    public ObjectExplorerViewModel(SqliteDatabaseInspector databaseInspector)
    {
        _databaseInspector = databaseInspector;
    }

    public ObservableCollection<DatabaseObjectNode> RootNodes { get; } = new();

    public async Task LoadAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        if (_databaseInspector is null)
        {
            throw new InvalidOperationException("A database inspector is required to load objects.");
        }

        var tables = await _databaseInspector.GetTablesAsync(databasePath, cancellationToken);
        var tableNodes = tables
            .Select(tableName => new DatabaseObjectNode(
                Id: $"table:{tableName}",
                Kind: "table",
                Name: tableName,
                ParentId: "group:tables",
                Children: Array.Empty<DatabaseObjectNode>()))
            .ToArray();

        RootNodes.Clear();
        RootNodes.Add(new DatabaseObjectNode(
            Id: "group:tables",
            Kind: "group",
            Name: "表",
            Children: tableNodes));

        SelectedNode = tableNodes.FirstOrDefault();
    }
}
