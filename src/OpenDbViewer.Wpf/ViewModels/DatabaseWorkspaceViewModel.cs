using CommunityToolkit.Mvvm.ComponentModel;
using OpenDbViewer.Domain.Models;
using System.IO;

namespace OpenDbViewer.Shell.ViewModels;

public partial class DatabaseWorkspaceViewModel : ObservableObject
{
    [ObservableProperty]
    private string databasePath = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    public DatabaseWorkspaceViewModel(
        ObjectExplorerViewModel objectExplorer,
        SchemaViewModel schema,
        DataViewModel data)
    {
        ObjectExplorer = objectExplorer;
        Schema = schema;
        Data = data;
    }

    public ObjectExplorerViewModel ObjectExplorer { get; }

    public SchemaViewModel Schema { get; }

    public DataViewModel Data { get; }

    public virtual async Task LoadAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        DatabasePath = databasePath;
        Title = Path.GetFileNameWithoutExtension(databasePath);

        await ObjectExplorer.LoadAsync(databasePath, cancellationToken);

        if (ObjectExplorer.SelectedNode is not null)
        {
            await SelectNodeAsync(ObjectExplorer.SelectedNode, cancellationToken);
        }
        else
        {
            Schema.Clear();
            Data.Clear();
        }
    }

    public async Task SelectNodeAsync(DatabaseObjectNode? node, CancellationToken cancellationToken = default)
    {
        ObjectExplorer.SelectedNode = node;

        if (node is null || !string.Equals(node.Kind, "table", StringComparison.OrdinalIgnoreCase))
        {
            Schema.Clear();
            Data.Clear();
            return;
        }

        await Schema.LoadAsync(DatabasePath, node.Name, cancellationToken);
        await Data.LoadFirstPageAsync(DatabasePath, node.Name, cancellationToken);
    }
}
