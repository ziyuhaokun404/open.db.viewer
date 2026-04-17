using CommunityToolkit.Mvvm.ComponentModel;
using OpenDbViewer.Domain.Models;
using OpenDbViewer.Infrastructure.Sqlite.Sqlite;
using System.Collections.ObjectModel;
using System.Data;

namespace OpenDbViewer.Shell.ViewModels;

public partial class DataViewModel : ObservableObject
{
    private readonly SqliteTableDataReader? _tableDataReader;

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

    public DataViewModel()
    {
    }

    public DataViewModel(SqliteTableDataReader tableDataReader)
    {
        _tableDataReader = tableDataReader;
    }

    public ObservableCollection<string> Columns { get; } = new();

    public ObservableCollection<DataRowViewModel> Rows { get; } = new();

    public async Task LoadFirstPageAsync(string databasePath, string tableName, CancellationToken cancellationToken = default)
    {
        if (_tableDataReader is null)
        {
            throw new InvalidOperationException("A table data reader is required to load data.");
        }

        var page = await _tableDataReader.ReadPageAsync(databasePath, tableName, 1, PageSize, cancellationToken: cancellationToken);
        ApplyPage(page);
    }

    public void Clear()
    {
        Columns.Clear();
        Rows.Clear();
        TableView = null;
        PageNumber = 0;
        HasNextPage = false;
        SortColumn = null;
        SortDirection = null;
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
