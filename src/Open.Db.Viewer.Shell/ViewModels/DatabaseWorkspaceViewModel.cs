using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Domain.Models;
using System.IO;
using System.Globalization;

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

    [ObservableProperty]
    private string databaseFileName = string.Empty;

    [ObservableProperty]
    private string databaseFileSizeSummary = "-";

    [ObservableProperty]
    private string databaseCreatedAtSummary = "-";

    [ObservableProperty]
    private string databaseUpdatedAtSummary = "-";

    [ObservableProperty]
    private string connectionStatusText = "未连接";

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

    public bool HasOpenDatabase => !string.IsNullOrWhiteSpace(DatabasePath);

    public string EmptyStateTitle => "尚未打开数据库";

    public string EmptyStateDescription => "从左侧导航或首页动作中打开一个 SQLite 数据库。";

    public bool HasTableSelection =>
        ObjectExplorer.SelectedNode is not null &&
        string.Equals(ObjectExplorer.SelectedNode.Kind, "table", StringComparison.OrdinalIgnoreCase);

    public string SelectedObjectTitle => ObjectExplorer.SelectedNode?.Name ?? "未选择数据表";

    public string SelectedObjectSubtitle => HasTableSelection
        ? $"{Schema.ColumnCountSummary} · {Schema.RowCountSummary} · {Schema.IndexSummary}"
        : "请先从左侧导航选择一个表。";

    public string DatabaseSummary => HasOpenDatabase
        ? $"{DatabaseFileSizeSummary} · {ObjectExplorer.TotalObjectCount} 个数据表"
        : "尚未连接数据库";

    public string SelectedObjectKindLabel => HasTableSelection
        ? "表 (TABLE)"
        : "未选择对象";

    public string WorkspaceStatusSummary => HasTableSelection
        ? $"结构分析就绪 · {Schema.TriggerSummary}"
        : "等待选择对象";

    public string MetricRowCountText => HasTableSelection
        ? Schema.RowCount.ToString("N0", CultureInfo.InvariantCulture)
        : "-";

    public string MetricColumnCountText => HasTableSelection
        ? Schema.Columns.Count.ToString(CultureInfo.InvariantCulture)
        : "-";

    public string MetricPageSizeText => Schema.PageSizeBytes <= 0
        ? "-"
        : $"{Schema.PageSizeBytes:N0} 字节";

    public string MetricCreatedDateText => ToMetricDate(DatabaseCreatedAtSummary);

    public string MetricUpdatedDateText => ToMetricDate(DatabaseUpdatedAtSummary);

    public string FooterStatusText => HasOpenDatabase ? "就绪" : "未连接";

    public string FooterEngineText => "SQLite";

    public string FooterAccessModeText => "只读";

    public string FooterRowCountText => HasTableSelection ? $"行: {Schema.RowCount:N0}" : "行: -";

    public string FooterColumnCountText => HasTableSelection ? $"列: {Schema.Columns.Count}" : "列: -";

    public virtual async Task LoadAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        DatabasePath = databasePath;
        Title = Path.GetFileNameWithoutExtension(databasePath);
        UpdateDatabaseFileMetadata(databasePath);
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

        ConnectionStatusText = HasOpenDatabase ? "连接正常" : "未连接";
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
            OnPropertyChanged(nameof(HasOpenDatabase));
            NotifyWorkspaceStateChanged();
            return;
        }

        await Schema.LoadAsync(DatabasePath, node.Name, cancellationToken);
        await Data.LoadFirstPageAsync(DatabasePath, node.Name, cancellationToken);
        Query.Configure(DatabasePath, node.Name);
        StatusMessage = $"已加载 {node.Name} · {Schema.RowCountSummary}。";
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
            UpdateDatabaseFileMetadata(DatabasePath);

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
            ConnectionStatusText = "连接正常";
        }
        finally
        {
            IsRefreshing = false;
            NotifyWorkspaceStateChanged();
        }
    }

    private void NotifyWorkspaceStateChanged()
    {
        OnPropertyChanged(nameof(HasOpenDatabase));
        OnPropertyChanged(nameof(HasTableSelection));
        OnPropertyChanged(nameof(SelectedObjectTitle));
        OnPropertyChanged(nameof(SelectedObjectSubtitle));
        OnPropertyChanged(nameof(DatabaseSummary));
        OnPropertyChanged(nameof(SelectedObjectKindLabel));
        OnPropertyChanged(nameof(WorkspaceStatusSummary));
        OnPropertyChanged(nameof(MetricRowCountText));
        OnPropertyChanged(nameof(MetricColumnCountText));
        OnPropertyChanged(nameof(MetricPageSizeText));
        OnPropertyChanged(nameof(MetricCreatedDateText));
        OnPropertyChanged(nameof(MetricUpdatedDateText));
        OnPropertyChanged(nameof(FooterStatusText));
        OnPropertyChanged(nameof(FooterEngineText));
        OnPropertyChanged(nameof(FooterAccessModeText));
        OnPropertyChanged(nameof(FooterRowCountText));
        OnPropertyChanged(nameof(FooterColumnCountText));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateDescription));
    }

    private void UpdateDatabaseFileMetadata(string databasePath)
    {
        if (!File.Exists(databasePath))
        {
            DatabaseFileName = Path.GetFileName(databasePath);
            DatabaseFileSizeSummary = "-";
            DatabaseCreatedAtSummary = "-";
            DatabaseUpdatedAtSummary = "-";
            return;
        }

        var fileInfo = new FileInfo(databasePath);
        DatabaseFileName = fileInfo.Name;
        DatabaseFileSizeSummary = FormatFileSize(fileInfo.Length);
        DatabaseCreatedAtSummary = fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        DatabaseUpdatedAtSummary = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    private static string FormatFileSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var unitIndex = 0;
        decimal displaySize = bytes;

        while (displaySize >= 1024 && unitIndex < units.Length - 1)
        {
            displaySize /= 1024;
            unitIndex++;
        }

        var format = unitIndex == 0 ? "0" : "0.##";
        return $"{displaySize.ToString(format, CultureInfo.InvariantCulture)} {units[unitIndex]}";
    }

    private static string ToMetricDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == "-")
        {
            return "-";
        }

        return value.Length >= 10 ? value[..10] : value;
    }
}
