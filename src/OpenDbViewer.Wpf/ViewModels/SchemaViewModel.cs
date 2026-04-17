using CommunityToolkit.Mvvm.ComponentModel;
using OpenDbViewer.Domain.Models;
using OpenDbViewer.Infrastructure.Sqlite.Sqlite;
using System.Collections.ObjectModel;

namespace OpenDbViewer.Shell.ViewModels;

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
    }

    public void Clear()
    {
        TableName = string.Empty;
        Columns.Clear();
    }
}
