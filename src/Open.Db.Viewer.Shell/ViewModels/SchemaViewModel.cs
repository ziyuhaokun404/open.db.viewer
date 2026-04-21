using CommunityToolkit.Mvvm.ComponentModel;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;
using System.Collections.ObjectModel;

namespace Open.Db.Viewer.Shell.ViewModels;

public partial class SchemaViewModel : ObservableObject
{
    private readonly SqliteDatabaseInspector? _databaseInspector;

    [ObservableProperty]
    private string tableName = string.Empty;

    public SchemaViewModel()
    {
    }

    public SchemaViewModel(SqliteDatabaseInspector databaseInspector)
    {
        _databaseInspector = databaseInspector;
    }

    public ObservableCollection<TableColumnInfo> Columns { get; } = new();

    public bool HasColumns => Columns.Count > 0;

    public string ColumnCountSummary => Columns.Count switch
    {
        1 => "1 列",
        _ => $"{Columns.Count} 列"
    };

    public async Task LoadAsync(string databasePath, string tableName, CancellationToken cancellationToken = default)
    {
        if (_databaseInspector is null)
        {
            throw new InvalidOperationException("A database inspector is required to load schema.");
        }

        var schema = await _databaseInspector.GetSchemaAsync(databasePath, tableName, cancellationToken);

        TableName = schema.TableName;
        Columns.Clear();
        foreach (var column in schema.Columns)
        {
            Columns.Add(column);
        }

        OnPropertyChanged(nameof(HasColumns));
        OnPropertyChanged(nameof(ColumnCountSummary));
    }

    public void Clear()
    {
        TableName = string.Empty;
        Columns.Clear();
        OnPropertyChanged(nameof(HasColumns));
        OnPropertyChanged(nameof(ColumnCountSummary));
    }
}
