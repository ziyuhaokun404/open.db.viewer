using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;
using Open.Db.Viewer.ShellHost.Services;

using System.Collections.ObjectModel;
using System.Data;
using System.IO;

namespace Open.Db.Viewer.Shell.ViewModels;

public partial class DataViewModel : ObservableObject
{
    private static readonly int[] DefaultPageSizeOptions = [50, 100, 200, 500];
    private readonly SqliteTableDataReader? _tableDataReader;
    private readonly ExportService? _exportService;
    private readonly IFileDialogService? _fileDialogService;
    private string? _databasePath;
    private string? _tableName;

    [ObservableProperty]
    private int pageNumber;

    [ObservableProperty]
    private int pageSize = 100;

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
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = "请选择一个表以浏览数据行。";

    public DataViewModel()
    {
    }

    public DataViewModel(SqliteTableDataReader tableDataReader)
        : this(tableDataReader, null, null)
    {
    }

    public DataViewModel(SqliteTableDataReader tableDataReader, ExportService? exportService, IFileDialogService? fileDialogService)
    {
        _tableDataReader = tableDataReader;
        _exportService = exportService;
        _fileDialogService = fileDialogService;
    }

    public ObservableCollection<string> Columns { get; } = new();

    public ObservableCollection<DataRowViewModel> Rows { get; } = new();

    public IReadOnlyList<int> PageSizeOptions { get; } = DefaultPageSizeOptions;

    public bool HasRows => Rows.Count > 0;

    public string ResultSummary =>
        PageNumber == 0
            ? "尚未加载表数据。"
            : $"第 {PageNumber} 页 · {Columns.Count} 列 · 当前显示 {Rows.Count} 行";

    public string SortSummary => string.IsNullOrWhiteSpace(SortColumn)
        ? "默认排序"
        : $"{SortColumn} · {(string.Equals(SortDirection, "DESC", StringComparison.OrdinalIgnoreCase) ? "降序" : "升序")}";

    public async Task LoadFirstPageAsync(string databasePath, string tableName, CancellationToken cancellationToken = default)
    {
        if (_tableDataReader is null)
        {
            throw new InvalidOperationException("A table data reader is required to load data.");
        }

        _databasePath = databasePath;
        _tableName = tableName;

        await LoadPageAsync(1, cancellationToken);
    }

    public async Task ChangePageSizeAsync(int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize));
        }

        PageSize = pageSize;

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

    [RelayCommand]
    public Task RefreshCurrentPageAsync(CancellationToken cancellationToken = default) =>
        PageNumber > 0 ? LoadPageAsync(PageNumber, cancellationToken) : Task.CompletedTask;

    [RelayCommand]
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

        StatusMessage = $"已将当前页导出到 {Path.GetFileName(exportPath)}。";
    }

    public void Clear()
    {
        Columns.Clear();
        Rows.Clear();
        TableView = null;
        PageNumber = 0;
        HasPreviousPage = false;
        HasNextPage = false;
        SortColumn = null;
        SortDirection = null;
        _databasePath = null;
        _tableName = null;
        StatusMessage = "请选择一个表以浏览数据行。";
        NotifyStateChanged();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        LoadPreviousPageCommand.NotifyCanExecuteChanged();
        LoadNextPageCommand.NotifyCanExecuteChanged();
    }

    private bool CanLoadPreviousPage() => !IsLoading && HasPreviousPage;

    private bool CanLoadNextPage() => !IsLoading && HasNextPage;

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
                cancellationToken: cancellationToken);
            ApplyPage(page);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyPage(TablePageResult page)
    {
        Columns.Clear();
        foreach (var column in page.Columns)
        {
            Columns.Add(column);
        }

        Rows.Clear();
        foreach (var row in page.Rows)
        {
            Rows.Add(new DataRowViewModel(row));
        }

        var table = new DataTable();
        foreach (var column in page.Columns)
        {
            table.Columns.Add(column);
        }

        foreach (var row in page.Rows)
        {
            table.Rows.Add(row.ToArray());
        }

        TableView = table.DefaultView;
        PageNumber = page.PageNumber;
        PageSize = page.PageSize;
        HasPreviousPage = page.PageNumber > 1;
        HasNextPage = page.HasNextPage;
        SortColumn = page.SortColumn;
        SortDirection = page.SortDirection;
        StatusMessage = $"已从 {(_tableName ?? "数据表")} 加载 {Rows.Count} 行数据。";
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(ResultSummary));
        OnPropertyChanged(nameof(SortSummary));
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
