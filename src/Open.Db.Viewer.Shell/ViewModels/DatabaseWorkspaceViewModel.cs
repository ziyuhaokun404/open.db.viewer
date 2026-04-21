using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Domain.Models;
using System.IO;

namespace Open.Db.Viewer.Shell.ViewModels;

public partial class DatabaseWorkspaceViewModel : ObservableObject
{
    [ObservableProperty]
    private string databasePath = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Select a table to start browsing.";

    [ObservableProperty]
    private bool isRefreshing;

    public Func<Task>? RequestReturnHomeAsync { get; set; }

    public DatabaseWorkspaceViewModel(
        ObjectExplorerViewModel objectExplorer,
        SchemaViewModel schema,
        DataViewModel data,
        QueryViewModel query)
    {
        ObjectExplorer = objectExplorer;
        Schema = schema;
        Data = data;
        Query = query;
    }

    public ObjectExplorerViewModel ObjectExplorer { get; }

    public SchemaViewModel Schema { get; }

    public DataViewModel Data { get; }

    public QueryViewModel Query { get; }

    public bool HasTableSelection =>
        ObjectExplorer.SelectedNode is not null &&
        string.Equals(ObjectExplorer.SelectedNode.Kind, "table", StringComparison.OrdinalIgnoreCase);

    public string SelectedObjectTitle => ObjectExplorer.SelectedNode?.Name ?? "No table selected";

    public string SelectedObjectSubtitle => HasTableSelection
        ? "Structure, data, and query tools are ready."
        : "Choose a table from the left navigation to begin.";

    public virtual async Task LoadAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        DatabasePath = databasePath;
        Title = Path.GetFileNameWithoutExtension(databasePath);
        Query.Configure(databasePath);

        await ObjectExplorer.LoadAsync(databasePath, cancellationToken);

        if (ObjectExplorer.SelectedNode is not null)
        {
            await SelectNodeAsync(ObjectExplorer.SelectedNode, cancellationToken);
        }
        else
        {
            Schema.Clear();
            Data.Clear();
            StatusMessage = "Select a table to start browsing.";
        }

        NotifyWorkspaceStateChanged();
    }

    public async Task SelectNodeAsync(DatabaseObjectNode? node, CancellationToken cancellationToken = default)
    {
        ObjectExplorer.SelectedNode = node;

        if (node is null || !string.Equals(node.Kind, "table", StringComparison.OrdinalIgnoreCase))
        {
            Schema.Clear();
            Data.Clear();
            Query.Configure(DatabasePath);
            StatusMessage = "Select a table to start browsing.";
            NotifyWorkspaceStateChanged();
            return;
        }

        await Schema.LoadAsync(DatabasePath, node.Name, cancellationToken);
        await Data.LoadFirstPageAsync(DatabasePath, node.Name, cancellationToken);
        Query.Configure(DatabasePath, node.Name);
        StatusMessage = $"Loaded {node.Name}.";
        NotifyWorkspaceStateChanged();
    }

    [RelayCommand]
    public async Task ReturnHomeAsync()
    {
        if (RequestReturnHomeAsync is not null)
        {
            await RequestReturnHomeAsync();
        }
    }

    [RelayCommand]
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(DatabasePath))
        {
            return;
        }

        IsRefreshing = true;
        try
        {
            var selectedTableName = HasTableSelection ? ObjectExplorer.SelectedNode!.Name : null;
            await ObjectExplorer.LoadAsync(DatabasePath, cancellationToken);

            DatabaseObjectNode? restoredNode = null;
            if (!string.IsNullOrWhiteSpace(selectedTableName))
            {
                restoredNode = ObjectExplorer.RootNodes
                    .SelectMany(root => root.Children ?? Array.Empty<DatabaseObjectNode>())
                    .FirstOrDefault(node => node.Name.Equals(selectedTableName, StringComparison.OrdinalIgnoreCase));
            }

            if (restoredNode is not null)
            {
                await SelectNodeAsync(restoredNode, cancellationToken);
            }
            else if (ObjectExplorer.SelectedNode is not null)
            {
                await SelectNodeAsync(ObjectExplorer.SelectedNode, cancellationToken);
            }
            else
            {
                Schema.Clear();
                Data.Clear();
                Query.Configure(DatabasePath);
                NotifyWorkspaceStateChanged();
            }

            StatusMessage = "Workspace refreshed.";
        }
        finally
        {
            IsRefreshing = false;
            NotifyWorkspaceStateChanged();
        }
    }

    private void NotifyWorkspaceStateChanged()
    {
        OnPropertyChanged(nameof(HasTableSelection));
        OnPropertyChanged(nameof(SelectedObjectTitle));
        OnPropertyChanged(nameof(SelectedObjectSubtitle));
    }
}
