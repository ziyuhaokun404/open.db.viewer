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
    private string statusMessage = "请选择一个表开始浏览。";

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

    public string SelectedObjectTitle => ObjectExplorer.SelectedNode?.Name ?? "未选择数据表";

    public string SelectedObjectSubtitle => HasTableSelection
        ? "表结构、数据和查询工具已就绪。"
        : "请先从左侧导航选择一个表。";

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
            StatusMessage = "请选择一个表开始浏览。";
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
            StatusMessage = "请选择一个表开始浏览。";
            NotifyWorkspaceStateChanged();
            return;
        }

        await Schema.LoadAsync(DatabasePath, node.Name, cancellationToken);
        await Data.LoadFirstPageAsync(DatabasePath, node.Name, cancellationToken);
        Query.Configure(DatabasePath, node.Name);
        StatusMessage = $"已加载 {node.Name}。";
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

            StatusMessage = "工作区已刷新。";
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
