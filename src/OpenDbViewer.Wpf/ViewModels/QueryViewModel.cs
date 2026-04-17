using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDbViewer.Application.Services;
using OpenDbViewer.Domain.Models;
using OpenDbViewer.Shell.Services;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;

namespace OpenDbViewer.Shell.ViewModels;

public partial class QueryViewModel : ObservableObject
{
    public const string ReadyStatusMessage = "Ready to run SQL.";

    private readonly QueryService _queryService;
    private readonly ExportService _exportService;
    private readonly IFileDialogService _fileDialogService;

    private string? _templateTableName;

    [ObservableProperty]
    private string databasePath = string.Empty;

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

    public void Configure(string databasePath, string? tableName = null)
    {
        DatabasePath = databasePath;

        if (tableName is not null)
        {
            _templateTableName = tableName;
            QueryText = BuildDefaultQuery(tableName);
        }

        ClearResults();
        StatusMessage = ReadyStatusMessage;
    }

    [RelayCommand]
    public async Task ExecuteQueryAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(DatabasePath) || string.IsNullOrWhiteSpace(QueryText))
        {
            StatusMessage = "Choose a database and enter SQL first.";
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
            : "query-results.csv";
        var exportPath = _fileDialogService.PickCsvSavePath(suggestedName);
        if (string.IsNullOrWhiteSpace(exportPath))
        {
            return;
        }

        await _exportService.ExportAsync(
            exportPath,
            new TabularData(Columns.ToArray(), Rows.Select(row => row.Values).ToArray()),
            cancellationToken);

        StatusMessage = $"Exported results to {Path.GetFileName(exportPath)}.";
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

        HasResults = result.Columns.Count > 0 || result.Rows.Count > 0;

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
    }

    private void ClearResults()
    {
        Columns.Clear();
        Rows.Clear();
        ResultView = null;
        HasResults = false;
    }

    private static string BuildDefaultQuery(string tableName) => $"select * from \"{tableName}\" limit 100;";

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
}
