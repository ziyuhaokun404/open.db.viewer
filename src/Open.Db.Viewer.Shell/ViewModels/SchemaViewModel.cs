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

    [ObservableProperty]
    private long rowCount;

    [ObservableProperty]
    private int pageSizeBytes;

    [ObservableProperty]
    private string sqliteVersion = string.Empty;

    [ObservableProperty]
    private string encoding = string.Empty;

    [ObservableProperty]
    private int userVersion;

    [ObservableProperty]
    private string createSql = string.Empty;

    public SchemaViewModel()
    {
    }

    public SchemaViewModel(SqliteDatabaseInspector databaseInspector)
    {
        _databaseInspector = databaseInspector;
    }

    public ObservableCollection<TableColumnInfo> Columns { get; } = new();

    public ObservableCollection<SchemaColumnItemViewModel> ColumnItems { get; } = new();

    public ObservableCollection<DatabaseScriptItem> Indexes { get; } = new();

    public ObservableCollection<DatabaseScriptItem> Triggers { get; } = new();

    public bool HasColumns => Columns.Count > 0;

    public bool HasIndexes => Indexes.Count > 0;

    public bool HasTriggers => Triggers.Count > 0;

    public bool HasCreateSql => !string.IsNullOrWhiteSpace(CreateSql);

    public string ColumnCountSummary => Columns.Count switch
    {
        1 => "1 列",
        _ => $"{Columns.Count} 列"
    };

    public string RowCountSummary => $"{RowCount:N0} 行";

    public string PageSizeSummary => PageSizeBytes <= 0
        ? "未知"
        : $"{PageSizeBytes:N0} B";

    public string EncodingSummary => string.IsNullOrWhiteSpace(Encoding)
        ? "-"
        : Encoding;

    public string UserVersionSummary => UserVersion.ToString();

    public string IndexSummary => Indexes.Count switch
    {
        1 => "1 个索引",
        _ => $"{Indexes.Count} 个索引"
    };

    public string TriggerSummary => Triggers.Count switch
    {
        1 => "1 个触发器",
        _ => $"{Triggers.Count} 个触发器"
    };

    public async Task LoadAsync(string databasePath, string tableName, CancellationToken cancellationToken = default)
    {
        if (_databaseInspector is null)
        {
            throw new InvalidOperationException("A database inspector is required to load schema.");
        }

        var schemaTask = _databaseInspector.GetSchemaAsync(databasePath, tableName, cancellationToken);
        var metadataTask = _databaseInspector.GetTableMetadataAsync(databasePath, tableName, cancellationToken);

        await Task.WhenAll(schemaTask, metadataTask);

        var schema = await schemaTask;
        var metadata = await metadataTask;

        TableName = schema.TableName;
        RowCount = metadata.RowCount;
        PageSizeBytes = metadata.PageSizeBytes;
        SqliteVersion = metadata.SqliteVersion;
        Encoding = metadata.Encoding;
        UserVersion = metadata.UserVersion;
        CreateSql = metadata.CreateSql ?? string.Empty;

        Columns.Clear();
        ColumnItems.Clear();
        foreach (var column in schema.Columns)
        {
            Columns.Add(column);
            ColumnItems.Add(SchemaColumnItemViewModel.From(column, ColumnItems.Count + 1));
        }

        Indexes.Clear();
        foreach (var script in metadata.Indexes)
        {
            Indexes.Add(script);
        }

        Triggers.Clear();
        foreach (var script in metadata.Triggers)
        {
            Triggers.Add(script);
        }

        NotifyStateChanged();
    }

    public void Clear()
    {
        TableName = string.Empty;
        RowCount = 0;
        PageSizeBytes = 0;
        SqliteVersion = string.Empty;
        Encoding = string.Empty;
        UserVersion = 0;
        CreateSql = string.Empty;
        Columns.Clear();
        ColumnItems.Clear();
        Indexes.Clear();
        Triggers.Clear();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(HasColumns));
        OnPropertyChanged(nameof(HasIndexes));
        OnPropertyChanged(nameof(HasTriggers));
        OnPropertyChanged(nameof(HasCreateSql));
        OnPropertyChanged(nameof(ColumnCountSummary));
        OnPropertyChanged(nameof(RowCountSummary));
        OnPropertyChanged(nameof(PageSizeSummary));
        OnPropertyChanged(nameof(EncodingSummary));
        OnPropertyChanged(nameof(UserVersionSummary));
        OnPropertyChanged(nameof(IndexSummary));
        OnPropertyChanged(nameof(TriggerSummary));
    }
}

public sealed record SchemaColumnItemViewModel(
    int Ordinal,
    string Name,
    string DataType,
    string Nullability,
    string PrimaryKey,
    string DefaultValue,
    string Description)
{
    public static SchemaColumnItemViewModel From(TableColumnInfo column, int ordinal)
    {
        var description = column.IsPrimaryKey
            ? "主键列"
            : column.IsNullable
                ? "允许为空"
                : "必填字段";

        return new SchemaColumnItemViewModel(
            ordinal,
            column.Name,
            string.IsNullOrWhiteSpace(column.DataType) ? "未声明" : column.DataType,
            column.IsNullable ? "是" : "否",
            column.IsPrimaryKey ? "是" : "否",
            string.IsNullOrWhiteSpace(column.DefaultValue) ? "-" : column.DefaultValue,
            description);
    }
}
