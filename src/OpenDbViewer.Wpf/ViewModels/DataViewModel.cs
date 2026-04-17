using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDbViewer.Domain.Models;
using OpenDbViewer.Infrastructure.Sqlite.Sqlite;
using System.Collections.ObjectModel;
using System.Data;

namespace OpenDbViewer.Shell.ViewModels;

public partial class DataViewModel : ObservableObject
{
    private static readonly int[] DefaultPageSizeOptions = [50, 100, 200, 500];
    private readonly SqliteTableDataReader? _tableDataReader;
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

    public DataViewModel()
    {
    }

    public DataViewModel(SqliteTableDataReader tableDataReader)
    {
        _tableDataReader = tableDataReader;
    }

    public ObservableCollection<string> Columns { get; } = new();

    public ObservableCollection<DataRowViewModel> Rows { get; } = new();

    public IReadOnlyList<int> PageSizeOptions { get; } = DefaultPageSizeOptions;

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
