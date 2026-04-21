using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.Services;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;

namespace Open.Db.Viewer.Shell.ViewModels;

public partial class QueryViewModel : ObservableObject
{
    public const string ReadyStatusMessage = "可以开始执行 SQL。";

    private readonly QueryService _queryService;
    private readonly ExportService _exportService;
    private readonly IFileDialogService _fileDialogService;

    private string? _templateTableName;

    [ObservableProperty]
    private string databasePath = string.Empty;

    [ObservableProperty]
    private string currentTableName = string.Empty;

    [ObservableProperty]
    private string queryText = string.Empty;

    [ObservableProperty]
    private string statusMessage = ReadyStatusMessage;

    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportResultsCommand))]
    private bool hasResults;

    [ObservableProperty]
    private DataView? resultView;

    public QueryViewModel(QueryService queryService, ExportService exportService, IFileDialogService fileDialogService)
    {
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(exportService);
        ArgumentNullException.ThrowIfNull(fileDialogService);

        _queryService = queryService;
        _exportService = exportService;
        _fileDialogService = fileDialogService;
    }

    public ObservableCollection<string> Columns { get; } = new();

    public ObservableCollection<DataRowViewModel> Rows { get; } = new();

    public string ResultSummary => Rows.Count == 0
        ? "未返回任何数据行。"
        : $"{Rows.Count} 行 · {Columns.Count} 列";

    public bool ShowEmptyResultState => !IsBusy && Columns.Count == 0 && Rows.Count == 0 && !string.IsNullOrWhiteSpace(DatabasePath);

    public bool ShowResultGrid => Columns.Count > 0 || Rows.Count > 0;

    public void Configure(string databasePath, string? tableName = null)
    {
        DatabasePath = databasePath;

        if (tableName is not null)
        {
            _templateTableName = tableName;
            CurrentTableName = tableName;
            QueryText = BuildSelectTemplate(tableName);
        }
        else
        {
            _templateTableName = null;
            CurrentTableName = string.Empty;
        }

        ClearResults();
        StatusMessage = ReadyStatusMessage;
        NotifyQueryStateChanged();
    }

    [RelayCommand]
    public async Task ExecuteQueryAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(DatabasePath) || string.IsNullOrWhiteSpace(QueryText))
        {
            StatusMessage = "请先选择数据库并输入 SQL。";
            return;
        }

        IsBusy = true;

        try
        {
            var result = await _queryService.ExecuteAsync(
                DatabasePath,
                new QueryExecutionRequest(QueryText),
                cancellationToken);

            ApplyResult(result);
            StatusMessage = $"{result.Message} ({result.Duration.TotalMilliseconds:F0} ms)";
        }
        catch (Exception exception)
        {
            ClearResults();
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyQueryStateChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportResults))]
    public async Task ExportResultsAsync(CancellationToken cancellationToken = default)
    {
        if (!HasResults)
        {
            return;
        }

        var suggestedName = _templateTableName is { Length: > 0 }
            ? $"{_templateTableName}.csv"
            : "查询结果.csv";
        var exportPath = _fileDialogService.PickCsvSavePath(suggestedName);
        if (string.IsNullOrWhiteSpace(exportPath))
        {
            return;
        }

        await _exportService.ExportAsync(
            exportPath,
            new TabularData(Columns.ToArray(), Rows.Select(row => row.Values).ToArray()),
            cancellationToken);

        StatusMessage = $"已将结果导出到 {Path.GetFileName(exportPath)}。";
    }

    [RelayCommand]
    public void UseSelectTemplate()
    {
        if (_templateTableName is null)
        {
            return;
        }

        QueryText = BuildSelectTemplate(_templateTableName);
        StatusMessage = ReadyStatusMessage;
    }

    [RelayCommand]
    public void UseCountTemplate()
    {
        if (_templateTableName is null)
        {
            return;
        }

        QueryText = BuildCountTemplate(_templateTableName);
        StatusMessage = ReadyStatusMessage;
    }

    [RelayCommand]
    public void UseSchemaTemplate()
    {
        if (_templateTableName is null)
        {
            return;
        }

        QueryText = BuildSchemaTemplate(_templateTableName);
        StatusMessage = ReadyStatusMessage;
    }

    private bool CanExportResults() => HasResults && !IsBusy;

    partial void OnIsBusyChanged(bool value) => ExportResultsCommand.NotifyCanExecuteChanged();

    private void ApplyResult(QueryExecutionResult result)
    {
        Columns.Clear();
        foreach (var column in result.Columns)
        {
            Columns.Add(column);
        }

        Rows.Clear();
        foreach (var row in result.Rows)
        {
            Rows.Add(new DataRowViewModel(row));
        }

        HasResults = result.Rows.Count > 0;

        var table = new DataTable();
        foreach (var column in result.Columns)
        {
            table.Columns.Add(column);
        }

        foreach (var row in result.Rows)
        {
            table.Rows.Add(row.Select(ToDisplayValue).ToArray());
        }

        ResultView = table.DefaultView;
        NotifyQueryStateChanged();
    }

    private void ClearResults()
    {
        Columns.Clear();
        Rows.Clear();
        ResultView = null;
        HasResults = false;
        NotifyQueryStateChanged();
    }

    private static string BuildSelectTemplate(string tableName) => $"select * from \"{tableName}\" limit 100;";

    private static string BuildCountTemplate(string tableName) => $"select count(*) as total_rows from \"{tableName}\";";

    private static string BuildSchemaTemplate(string tableName) => $"pragma table_info(\"{tableName}\");";

    private static object ToDisplayValue(object? value)
    {
        return value switch
        {
            null => string.Empty,
            byte[] bytes => Convert.ToHexString(bytes),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    private void NotifyQueryStateChanged()
    {
        OnPropertyChanged(nameof(ResultSummary));
        OnPropertyChanged(nameof(ShowEmptyResultState));
        OnPropertyChanged(nameof(ShowResultGrid));
    }
}
