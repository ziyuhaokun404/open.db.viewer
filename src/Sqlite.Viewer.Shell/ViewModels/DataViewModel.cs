using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Sqlite.Viewer.Application.Abstractions;
using Sqlite.Viewer.Application.Services;
using Sqlite.Viewer.Domain.Display;
using Sqlite.Viewer.Domain.Models;
using Sqlite.Viewer.Infrastructure.Sqlite.Sqlite;
using Sqlite.Viewer.ShellHost.Services;

using System.Collections.ObjectModel;
using System.Data;
using System.IO;

namespace Sqlite.Viewer.Shell.ViewModels;

public partial class DataViewModel : ObservableObject
{
    private static readonly int[] DefaultPageSizeOptions = [50, 100, 200, 500];
    private readonly SqliteTableDataReader? _tableDataReader;
    private readonly ExportService? _exportService;
    private readonly IFileDialogService? _fileDialogService;
    private readonly IAppSettingsStore? _settingsStore;
    private readonly IClipboardService _clipboardService;
    private string? _databasePath;
    private string? _tableName;
    private CancellationTokenSource? _exportCts;

    [ObservableProperty]
    private int pageNumber;

    [ObservableProperty]
    private int pageSize = AppSettings.DefaultPageSize;

    [ObservableProperty]
    private bool hasNextPage;

    [ObservableProperty]
    private string? sortColumn;

    [ObservableProperty]
    private string? sortDirection;

    [ObservableProperty]
    private DataView? tableView;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadPreviousPageCommand))]
    private bool hasPreviousPage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadNextPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(GoToPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportCurrentPageCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportFullTableCommand))]
    [NotifyCanExecuteChangedFor(nameof(ApplyFilterCommand))]
    private bool isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportFullTableCommand))]
    [NotifyCanExecuteChangedFor(nameof(CancelExportCommand))]
    private bool isExporting;

    [ObservableProperty]
    private string statusMessage = "请选择一个表以浏览数据行。";

    [ObservableProperty]
    private long totalRowCount;

    [ObservableProperty]
    private int totalPages;

    [ObservableProperty]
    private int jumpToPageNumber = 1;

    [ObservableProperty]
    private string? filterColumn;

    [ObservableProperty]
    private TableFilterOperator filterOperator = TableFilterOperator.Contains;

    [ObservableProperty]
    private string filterValue = string.Empty;

    [ObservableProperty]
    private bool hasActiveFilter;

    [ObservableProperty]
    private int selectedRowIndex = -1;

    [ObservableProperty]
    private int selectedColumnIndex = -1;

    private TableFilter? _activeFilter;

    public DataViewModel()
        : this(new ClipboardService())
    {
    }

    public DataViewModel(IClipboardService clipboardService)
    {
        ArgumentNullException.ThrowIfNull(clipboardService);
        _clipboardService = clipboardService;
    }

    public DataViewModel(SqliteTableDataReader tableDataReader)
        : this(tableDataReader, new ClipboardService())
    {
    }

    public DataViewModel(SqliteTableDataReader tableDataReader, IClipboardService clipboardService)
        : this(tableDataReader, clipboardService, null, null, null)
    {
    }

    public DataViewModel(
        SqliteTableDataReader tableDataReader,
        ExportService? exportService,
        IFileDialogService? fileDialogService,
        IAppSettingsStore? settingsStore = null)
        : this(tableDataReader, new ClipboardService(), exportService, fileDialogService, settingsStore)
    {
    }

    public DataViewModel(
        SqliteTableDataReader tableDataReader,
        IClipboardService clipboardService,
        ExportService? exportService,
        IFileDialogService? fileDialogService,
        IAppSettingsStore? settingsStore = null)
    {
        ArgumentNullException.ThrowIfNull(clipboardService);
        _tableDataReader = tableDataReader;
        _clipboardService = clipboardService;
        _exportService = exportService;
        _fileDialogService = fileDialogService;
        _settingsStore = settingsStore;
        if (_settingsStore is not null)
        {
            pageSize = _settingsStore.Current.DefaultPageSizeValue;
        }
    }

    public ObservableCollection<string> Columns { get; } = new();

    public ObservableCollection<DataRowViewModel> Rows { get; } = new();

    public IReadOnlyList<int> PageSizeOptions { get; } = DefaultPageSizeOptions;

    public IReadOnlyList<FilterOperatorOption> FilterOperatorOptions { get; } =
    [
        new(TableFilterOperator.Contains, "包含"),
        new(TableFilterOperator.Equals, "等于"),
        new(TableFilterOperator.IsNull, "为空"),
        new(TableFilterOperator.IsNotNull, "非空")
    ];

    public bool HasRows => Rows.Count > 0;

    public bool FilterValueEnabled =>
        FilterOperator is TableFilterOperator.Contains or TableFilterOperator.Equals;

    public string ResultSummary
    {
        get
        {
            if (PageNumber == 0)
            {
                return "尚未加载表数据。";
            }

            var filterTag = HasActiveFilter ? " · 已筛选" : string.Empty;
            if (TotalRowCount <= 0)
            {
                return $"第 {PageNumber} 页 · {Columns.Count} 列 · 无匹配行{filterTag}";
            }

            var start = ((PageNumber - 1) * (long)PageSize) + 1;
            var end = start + Math.Max(0, Rows.Count) - 1;
            if (Rows.Count == 0)
            {
                return $"第 {PageNumber}/{TotalPages} 页 · 共 {TotalRowCount:N0} 行 · {Columns.Count} 列{filterTag}";
            }

            return $"第 {PageNumber}/{TotalPages} 页 · {start:N0}–{end:N0} / 共 {TotalRowCount:N0} 行 · {Columns.Count} 列{filterTag}";
        }
    }

    public string SortSummary => string.IsNullOrWhiteSpace(SortColumn)
        ? "默认排序"
        : $"{SortColumn} · {(string.Equals(SortDirection, "DESC", StringComparison.OrdinalIgnoreCase) ? "降序" : "升序")}";

    public string ActiveFilterSummary => !HasActiveFilter || _activeFilter is null
        ? "未筛选"
        : _activeFilter.Operator switch
        {
            TableFilterOperator.IsNull => $"{_activeFilter.Column} IS NULL",
            TableFilterOperator.IsNotNull => $"{_activeFilter.Column} IS NOT NULL",
            TableFilterOperator.Equals => $"{_activeFilter.Column} = {_activeFilter.Value}",
            _ => $"{_activeFilter.Column} 包含 \"{_activeFilter.Value}\""
        };

    public async Task LoadFirstPageAsync(string databasePath, string tableName, CancellationToken cancellationToken = default)
    {
        if (_tableDataReader is null)
        {
            throw new InvalidOperationException("A table data reader is required to load data.");
        }

        _databasePath = databasePath;
        _tableName = tableName;
        _activeFilter = null;
        HasActiveFilter = false;
        FilterColumn = null;
        FilterValue = string.Empty;
        FilterOperator = TableFilterOperator.Contains;
        SelectedRowIndex = -1;
        SelectedColumnIndex = -1;

        await LoadPageAsync(1, cancellationToken);
    }

    public async Task ChangePageSizeAsync(int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        PageSize = pageSize;
        if (_settingsStore is not null)
        {
            var settings = _settingsStore.Current;
            settings.DefaultPageSizeValue = pageSize;
            await _settingsStore.SaveAsync(settings, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(_databasePath) || string.IsNullOrWhiteSpace(_tableName))
        {
            return;
        }

        await LoadPageAsync(1, cancellationToken);
    }

    public async Task ApplySortAsync(string columnName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);

        if (string.IsNullOrWhiteSpace(_databasePath) || string.IsNullOrWhiteSpace(_tableName))
        {
            return;
        }

        SortDirection = string.Equals(SortColumn, columnName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(SortDirection, "ASC", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";
        SortColumn = columnName;

        await LoadPageAsync(1, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanLoadPreviousPage))]
    public async Task LoadPreviousPageAsync(CancellationToken cancellationToken = default)
    {
        if (!CanLoadPreviousPage())
        {
            return;
        }

        await LoadPageAsync(PageNumber - 1, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanLoadNextPage))]
    public async Task LoadNextPageAsync(CancellationToken cancellationToken = default)
    {
        if (!CanLoadNextPage())
        {
            return;
        }

        await LoadPageAsync(PageNumber + 1, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanGoToPage))]
    public async Task GoToPageAsync(CancellationToken cancellationToken = default)
    {
        if (!CanGoToPage())
        {
            return;
        }

        var target = JumpToPageNumber;
        if (TotalPages > 0)
        {
            target = Math.Clamp(target, 1, TotalPages);
        }
        else
        {
            target = Math.Max(1, target);
        }

        JumpToPageNumber = target;
        await LoadPageAsync(target, cancellationToken);
    }

    [RelayCommand]
    public Task RefreshCurrentPageAsync(CancellationToken cancellationToken = default) =>
        PageNumber > 0 ? LoadPageAsync(PageNumber, cancellationToken) : Task.CompletedTask;

    [RelayCommand(CanExecute = nameof(CanApplyFilter))]
    public async Task ApplyFilterAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(FilterColumn))
        {
            StatusMessage = "请选择要筛选的列。";
            return;
        }

        if (FilterOperator is TableFilterOperator.Contains or TableFilterOperator.Equals &&
            FilterValue is null)
        {
            FilterValue = string.Empty;
        }

        _activeFilter = new TableFilter(FilterColumn, FilterOperator, FilterValue);
        HasActiveFilter = true;
        await LoadPageAsync(1, cancellationToken);
    }

    [RelayCommand]
    public async Task ClearFilterAsync(CancellationToken cancellationToken = default)
    {
        _activeFilter = null;
        HasActiveFilter = false;
        FilterValue = string.Empty;
        OnPropertyChanged(nameof(ActiveFilterSummary));

        if (string.IsNullOrWhiteSpace(_databasePath) || string.IsNullOrWhiteSpace(_tableName))
        {
            return;
        }

        await LoadPageAsync(1, cancellationToken);
    }

    [RelayCommand(CanExecute = nameof(CanExportCurrentPage))]
    public async Task ExportCurrentPageAsync(CancellationToken cancellationToken = default)
    {
        if (!HasRows || _exportService is null || _fileDialogService is null || string.IsNullOrWhiteSpace(_tableName))
        {
            return;
        }

        var exportPath = _fileDialogService.PickCsvSavePath($"{_tableName}-page-{PageNumber}.csv");
        if (string.IsNullOrWhiteSpace(exportPath))
        {
            return;
        }

        await _exportService.ExportAsync(
            exportPath,
            new TabularData(Columns.ToArray(), Rows.Select(row => row.Values).ToArray()),
            cancellationToken);

        StatusMessage = $"已将当前页导出到 {Path.GetFileName(exportPath)}（仅当前页）。";
    }

    [RelayCommand(CanExecute = nameof(CanExportFullTable))]
    public async Task ExportFullTableAsync(CancellationToken cancellationToken = default)
    {
        if (_tableDataReader is null ||
            _exportService is null ||
            _fileDialogService is null ||
            string.IsNullOrWhiteSpace(_databasePath) ||
            string.IsNullOrWhiteSpace(_tableName))
        {
            return;
        }

        var exportPath = _fileDialogService.PickCsvSavePath($"{_tableName}-full.csv");
        if (string.IsNullOrWhiteSpace(exportPath))
        {
            return;
        }

        _exportCts?.Cancel();
        _exportCts?.Dispose();
        _exportCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _exportCts.Token;

        IsExporting = true;
        StatusMessage = "正在导出整表…";
        try
        {
            var filters = GetActiveFilters();
            // 先取一页拿到列名（与筛选一致）。
            var firstPage = await _tableDataReader.ReadPageAsync(
                _databasePath,
                _tableName,
                pageNumber: 1,
                pageSize: 1,
                SortColumn,
                SortDirection,
                filters,
                token);

            var progress = new Progress<long>(written =>
            {
                StatusMessage = TotalRowCount > 0
                    ? $"正在导出整表… {written:N0} / {TotalRowCount:N0} 行"
                    : $"正在导出整表… 已写入 {written:N0} 行";
            });

            await _exportService.ExportStreamingAsync(
                exportPath,
                firstPage.Columns,
                StreamExportRowsAsync(filters, token),
                progress,
                token);

            StatusMessage = $"已将整表导出到 {Path.GetFileName(exportPath)}。";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "整表导出已取消。";
        }
        catch (Exception ex)
        {
            StatusMessage = $"整表导出失败：{ex.Message}";
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCancelExport))]
    public void CancelExport()
    {
        _exportCts?.Cancel();
        StatusMessage = "正在取消导出…";
    }

    [RelayCommand]
    public void CopyColumnNames()
    {
        if (Columns.Count == 0)
        {
            StatusMessage = "没有可复制的列名。";
            return;
        }

        _clipboardService.SetText(string.Join('\t', Columns));
        StatusMessage = "已复制列名。";
    }

    [RelayCommand]
    public void CopySelectedRow()
    {
        if (SelectedRowIndex < 0 || SelectedRowIndex >= Rows.Count)
        {
            StatusMessage = "请先选择一行。";
            return;
        }

        _clipboardService.SetText(CellDisplayFormatter.ToTsv(Rows[SelectedRowIndex].Values));
        StatusMessage = "已复制选中行（TSV）。";
    }

    [RelayCommand]
    public void CopySelectedCell()
    {
        if (SelectedRowIndex < 0 || SelectedRowIndex >= Rows.Count)
        {
            StatusMessage = "请先选择单元格。";
            return;
        }

        var row = Rows[SelectedRowIndex].Values;
        var columnIndex = SelectedColumnIndex;
        if (columnIndex < 0 || columnIndex >= row.Count)
        {
            StatusMessage = "请先选择单元格。";
            return;
        }

        _clipboardService.SetText(CellDisplayFormatter.FormatForExport(row[columnIndex]));
        StatusMessage = "已复制单元格。";
    }

    [RelayCommand]
    public void CopyCurrentPage()
    {
        if (!HasRows)
        {
            StatusMessage = "当前页没有数据。";
            return;
        }

        _clipboardService.SetText(
            CellDisplayFormatter.ToTsv(Columns.ToArray(), Rows.Select(r => r.Values)));
        StatusMessage = "已复制当前页（TSV，含列名）。";
    }

    public void Clear()
    {
        Columns.Clear();
        Rows.Clear();
        TableView = null;
        PageNumber = 0;
        TotalRowCount = 0;
        TotalPages = 0;
        JumpToPageNumber = 1;
        HasPreviousPage = false;
        HasNextPage = false;
        SortColumn = null;
        SortDirection = null;
        _activeFilter = null;
        HasActiveFilter = false;
        FilterColumn = null;
        FilterValue = string.Empty;
        SelectedRowIndex = -1;
        SelectedColumnIndex = -1;
        _databasePath = null;
        _tableName = null;
        StatusMessage = "请选择一个表以浏览数据行。";
        NotifyStateChanged();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        LoadPreviousPageCommand.NotifyCanExecuteChanged();
        LoadNextPageCommand.NotifyCanExecuteChanged();
        GoToPageCommand.NotifyCanExecuteChanged();
        ExportCurrentPageCommand.NotifyCanExecuteChanged();
        ExportFullTableCommand.NotifyCanExecuteChanged();
        ApplyFilterCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsExportingChanged(bool value)
    {
        ExportFullTableCommand.NotifyCanExecuteChanged();
        CancelExportCommand.NotifyCanExecuteChanged();
    }

    partial void OnFilterOperatorChanged(TableFilterOperator value)
    {
        OnPropertyChanged(nameof(FilterValueEnabled));
    }

    private bool CanLoadPreviousPage() => !IsLoading && HasPreviousPage;

    private bool CanLoadNextPage() => !IsLoading && HasNextPage;

    private bool CanGoToPage() => !IsLoading && PageNumber > 0 && !string.IsNullOrWhiteSpace(_tableName);

    private bool CanApplyFilter() => !IsLoading && !string.IsNullOrWhiteSpace(_tableName);

    private bool CanExportCurrentPage() => HasRows && !IsLoading && _exportService is not null;

    private bool CanExportFullTable() =>
        !IsLoading &&
        !IsExporting &&
        !string.IsNullOrWhiteSpace(_tableName) &&
        _exportService is not null &&
        _tableDataReader is not null;

    private bool CanCancelExport() => IsExporting;

    private async Task LoadPageAsync(int pageNumber, CancellationToken cancellationToken)
    {
        if (_tableDataReader is null)
        {
            throw new InvalidOperationException("A table data reader is required to load data.");
        }

        if (string.IsNullOrWhiteSpace(_databasePath) || string.IsNullOrWhiteSpace(_tableName))
        {
            throw new InvalidOperationException("A database and table must be selected before loading data.");
        }

        IsLoading = true;
        try
        {
            var page = await _tableDataReader.ReadPageAsync(
                _databasePath,
                _tableName,
                pageNumber,
                PageSize,
                SortColumn,
                SortDirection,
                GetActiveFilters(),
                cancellationToken: cancellationToken);
            ApplyPage(page);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private IReadOnlyList<TableFilter>? GetActiveFilters() =>
        _activeFilter is null ? null : [_activeFilter];

    private async IAsyncEnumerable<IReadOnlyList<object?>> StreamExportRowsAsync(
        IReadOnlyList<TableFilter>? filters,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var row in _tableDataReader!.StreamRowsAsync(
                           _databasePath!,
                           _tableName!,
                           SortColumn,
                           SortDirection,
                           filters,
                           cancellationToken))
        {
            yield return row;
        }
    }

    private void ApplyPage(TablePageResult page)
    {
        Columns.Clear();
        foreach (var column in page.Columns)
        {
            Columns.Add(column);
        }

        if (string.IsNullOrWhiteSpace(FilterColumn) && Columns.Count > 0)
        {
            FilterColumn = Columns[0];
        }

        Rows.Clear();
        foreach (var row in page.Rows)
        {
            Rows.Add(new DataRowViewModel(row));
        }

        var table = new DataTable();
        foreach (var column in page.Columns)
        {
            table.Columns.Add(column, typeof(string));
        }

        foreach (var row in page.Rows)
        {
            var displayValues = new object[row.Count];
            for (var i = 0; i < row.Count; i++)
            {
                displayValues[i] = CellDisplayFormatter.Format(row[i]);
            }

            table.Rows.Add(displayValues);
        }

        TableView = table.DefaultView;
        PageNumber = page.PageNumber;
        PageSize = page.PageSize;
        TotalRowCount = page.TotalRowCount;
        TotalPages = page.TotalPages;
        JumpToPageNumber = page.PageNumber;
        HasPreviousPage = page.PageNumber > 1;
        HasNextPage = page.HasNextPage;
        SortColumn = page.SortColumn;
        SortDirection = page.SortDirection;
        SelectedRowIndex = -1;
        SelectedColumnIndex = -1;
        StatusMessage = HasActiveFilter
            ? $"已加载 {(_tableName ?? "数据表")} · {Rows.Count} 行（筛选后共 {TotalRowCount:N0} 行）。"
            : $"已从 {(_tableName ?? "数据表")} 加载 {Rows.Count} 行数据（共 {TotalRowCount:N0} 行）。";
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(ResultSummary));
        OnPropertyChanged(nameof(SortSummary));
        OnPropertyChanged(nameof(ActiveFilterSummary));
        OnPropertyChanged(nameof(FilterValueEnabled));
        ExportCurrentPageCommand.NotifyCanExecuteChanged();
        ExportFullTableCommand.NotifyCanExecuteChanged();
    }
}

public sealed class DataRowViewModel
{
    public DataRowViewModel(IReadOnlyList<object?> values)
    {
        Values = values;
    }

    public IReadOnlyList<object?> Values { get; }
}

public sealed record FilterOperatorOption(TableFilterOperator Operator, string Label);
