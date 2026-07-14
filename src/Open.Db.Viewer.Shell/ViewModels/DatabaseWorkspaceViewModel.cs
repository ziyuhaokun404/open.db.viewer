using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.ShellHost.Services;
using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace Open.Db.Viewer.Shell.ViewModels;

public partial class DatabaseWorkspaceViewModel : ObservableObject
{
    private readonly IClipboardService _clipboardService;

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
        : this(objectExplorer, schema, data, query, new ClipboardService())
    {
    }

    public DatabaseWorkspaceViewModel(
        ObjectExplorerViewModel objectExplorer,
        SchemaViewModel schema,
        DataViewModel data,
        QueryViewModel query,
        IClipboardService clipboardService)
    {
        ArgumentNullException.ThrowIfNull(clipboardService);
        ObjectExplorer = objectExplorer;
        Schema = schema;
        Data = data;
        Query = query;
        _clipboardService = clipboardService;
        Query.PropertyChanged += OnQueryPropertyChanged;
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
        ObjectExplorer.SelectedNode.SupportsDataBrowse;

    public bool HasObjectSelection =>
        ObjectExplorer.SelectedNode is not null &&
        !ObjectExplorer.SelectedNode.IsGroup;

    public string SelectedObjectTitle => ObjectExplorer.SelectedNode?.Name ?? "未选择对象";

    public string SelectedObjectSubtitle => HasTableSelection
        ? $"{Schema.ColumnCountSummary} · {Schema.RowCountSummary} · {Schema.IndexSummary}"
        : HasObjectSelection
            ? ObjectExplorer.SelectedNode!.Subtitle
            : "请先从左侧导航选择一个对象。";

    public string DatabaseSummary => HasOpenDatabase
        ? $"{DatabaseFileSizeSummary} · {ObjectExplorer.TotalObjectCount} 个对象"
        : "尚未连接数据库";

    public string SelectedObjectKindLabel => HasObjectSelection
        ? ObjectExplorer.SelectedNode!.KindLabel
        : "未选择对象";

    public string WorkspaceStatusSummary => HasObjectSelection
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

    public string FooterAccessModeText => Query.AllowWriteMode ? "⚠ 查询可写" : "只读";

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

        if (node is null || node.IsGroup || !DatabaseObjectKinds.IsSchemaLoadable(node.Kind))
        {
            Schema.Clear();
            Data.Clear();
            Query.Configure(DatabasePath);
            StatusMessage = "请选择一个表、视图、索引或触发器。";
            OnPropertyChanged(nameof(HasOpenDatabase));
            NotifyWorkspaceStateChanged();
            return;
        }

        await Schema.LoadObjectAsync(DatabasePath, node, cancellationToken);

        if (node.SupportsDataBrowse)
        {
            try
            {
                await Data.LoadFirstPageAsync(DatabasePath, node.Name, cancellationToken);
            }
            catch (Exception ex)
            {
                Data.Clear();
                StatusMessage = $"已加载 {node.Name} 结构；数据页：{ex.Message}";
                Query.Configure(DatabasePath, node.Name);
                NotifyWorkspaceStateChanged();
                return;
            }

            Query.Configure(DatabasePath, node.Name);
            StatusMessage = $"已加载 {node.Name} · {Schema.RowCountSummary}。";
        }
        else
        {
            Data.Clear();
            Query.Configure(DatabasePath);
            Query.QueryText = node.Sql ?? $"-- {node.KindLabel}: {node.Name}";
            StatusMessage = $"已加载 {node.KindLabel} {node.Name}。";
        }

        NotifyWorkspaceStateChanged();
    }

    [RelayCommand]
    public void CopySelectedObjectName()
    {
        if (ObjectExplorer.SelectedNode is null || ObjectExplorer.SelectedNode.IsGroup)
        {
            return;
        }

        _clipboardService.SetText(ObjectExplorer.SelectedNode.Name);
        StatusMessage = $"已复制对象名：{ObjectExplorer.SelectedNode.Name}";
    }

    [RelayCommand]
    public void CopySelectedObjectSql()
    {
        var sql = ObjectExplorer.SelectedNode?.Sql ?? Schema.CreateSql;
        if (string.IsNullOrWhiteSpace(sql))
        {
            StatusMessage = "当前对象没有可复制的 DDL。";
            return;
        }

        _clipboardService.SetText(sql);
        StatusMessage = "已复制 DDL。";
    }

    [RelayCommand]
    public void OpenSelectedInQuery()
    {
        if (ObjectExplorer.SelectedNode is null || ObjectExplorer.SelectedNode.IsGroup)
        {
            return;
        }

        var node = ObjectExplorer.SelectedNode;
        if (node.SupportsDataBrowse)
        {
            Query.Configure(DatabasePath, node.Name);
        }
        else
        {
            Query.Configure(DatabasePath);
            Query.QueryText = node.Sql ?? string.Empty;
        }

        StatusMessage = $"已将 {node.Name} 填入查询页。";
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
            var selectedId = HasObjectSelection ? ObjectExplorer.SelectedNode!.Id : null;
            await ObjectExplorer.LoadAsync(DatabasePath, cancellationToken);
            UpdateDatabaseFileMetadata(DatabasePath);

            DatabaseObjectNode? restoredNode = null;
            if (!string.IsNullOrWhiteSpace(selectedId))
            {
                restoredNode = ObjectExplorer.FilteredTables
                    .FirstOrDefault(node => node.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase));
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
        OnPropertyChanged(nameof(HasObjectSelection));
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

    private void OnQueryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(QueryViewModel.AllowWriteMode) or null or "")
        {
            OnPropertyChanged(nameof(FooterAccessModeText));
        }
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
