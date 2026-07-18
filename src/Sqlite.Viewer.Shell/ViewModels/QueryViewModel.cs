using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Sqlite.Viewer.Application.Abstractions;
using Sqlite.Viewer.Application.Services;
using Sqlite.Viewer.Application.Sql;
using Sqlite.Viewer.Domain.Models;
using Sqlite.Viewer.Domain.Sqlite;
using Sqlite.Viewer.ShellHost.Services;

using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;

namespace Sqlite.Viewer.Shell.ViewModels;

public partial class QueryViewModel : ObservableObject
{
    public const string ReadyStatusMessage = "可以开始执行 SQL。";

    private readonly QueryService _queryService;
    private readonly ExportService _exportService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IDialogService _dialogService;
    private readonly IAppSettingsStore _settingsStore;
    private readonly IQueryHistoryStore _historyStore;

    private string? _templateTableName;
    private CancellationTokenSource? _executeCts;

    [ObservableProperty]
    private string databasePath = string.Empty;

    [ObservableProperty]
    private string currentTableName = string.Empty;

    [ObservableProperty]
    private string queryText = string.Empty;

    [ObservableProperty]
    private string statusMessage = ReadyStatusMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelQueryCommand))]
    [NotifyCanExecuteChangedFor(nameof(ExportResultsCommand))]
    private bool isBusy;

    [ObservableProperty]
    private bool allowWriteMode;

    /// <summary>本会话内对高风险 SQL 跳过重复确认（仍会在首次启用可写时确认）。</summary>
    [ObservableProperty]
    private bool skipHighRiskConfirmThisSession;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportResultsCommand))]
    private bool hasResults;

    [ObservableProperty]
    private bool isResultTruncated;

    [ObservableProperty]
    private DataView? resultView;

    [ObservableProperty]
    private int caretIndex = -1;

    [ObservableProperty]
    private QueryHistoryEntry? selectedHistoryEntry;

    [ObservableProperty]
    private QueryHistoryEntry? selectedFavoriteEntry;

    public QueryViewModel(
        QueryService queryService,
        ExportService exportService,
        IFileDialogService fileDialogService,
        IDialogService dialogService,
        IAppSettingsStore settingsStore,
        IQueryHistoryStore historyStore)
    {
        ArgumentNullException.ThrowIfNull(queryService);
        ArgumentNullException.ThrowIfNull(exportService);
        ArgumentNullException.ThrowIfNull(fileDialogService);
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(settingsStore);
        ArgumentNullException.ThrowIfNull(historyStore);

        _queryService = queryService;
        _exportService = exportService;
        _fileDialogService = fileDialogService;
        _dialogService = dialogService;
        _settingsStore = settingsStore;
        _historyStore = historyStore;
    }

    public ObservableCollection<string> Columns { get; } = new();

    public ObservableCollection<DataRowViewModel> Rows { get; } = new();

    public ObservableCollection<QueryHistoryEntry> RecentHistory { get; } = new();

    public ObservableCollection<QueryHistoryEntry> Favorites { get; } = new();

    public string ResultSummary
    {
        get
        {
            if (Rows.Count == 0)
            {
                return "未返回任何数据行。";
            }

            return IsResultTruncated
                ? $"{Rows.Count} 行 · {Columns.Count} 列 · 结果已截断"
                : $"{Rows.Count} 行 · {Columns.Count} 列";
        }
    }

    public bool ShowEmptyResultState => !IsBusy && Columns.Count == 0 && Rows.Count == 0 && !string.IsNullOrWhiteSpace(DatabasePath);

    public bool ShowResultGrid => Columns.Count > 0 || Rows.Count > 0;

    public bool ShowTruncationBanner => IsResultTruncated;

    public string QueryAccessModeLabel => AllowWriteMode ? "⚠ 可写模式" : "只读模式";

    public string QueryAccessModeSummary => AllowWriteMode
        ? (SkipHighRiskConfirmThisSession
            ? "可写连接已启用；本会话已跳过高风险二次确认，请谨慎执行。"
            : "可写连接已启用；DDL / VACUUM / 写 PRAGMA 等将二次确认。")
        : "当前 SQL 使用只读连接执行，写入和 DDL 会被拦截。";

    public string ToggleWriteModeText => AllowWriteMode ? "切回只读" : "启用可写";

    public bool ShowWriteWarning => AllowWriteMode;

    public string WriteModeBannerText => SkipHighRiskConfirmThisSession
        ? "可写模式已启用（本会话跳过高风险确认）— SQL 可能修改数据库结构和数据。"
        : "可写模式已启用 — SQL 可能修改数据库结构和数据。高风险操作将弹出确认。";

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
        _ = RefreshHistoryAsync();
    }

    [RelayCommand]
    public Task ExecuteQueryAsync() => ExecuteSqlCoreAsync(ResolveExecutableSql(), recordHistory: true);

    [RelayCommand]
    public Task ExecuteCurrentStatementAsync() =>
        ExecuteSqlCoreAsync(SqlStatementSplitter.ResolveStatementToExecute(QueryText, CaretIndex), recordHistory: true);

    [RelayCommand]
    public Task ExplainCurrentStatementAsync()
    {
        var statement = SqlStatementSplitter.ResolveStatementToExecute(QueryText, CaretIndex);
        if (string.IsNullOrWhiteSpace(statement))
        {
            StatusMessage = "没有可分析的 SQL 语句。";
            return Task.CompletedTask;
        }

        var trimmed = statement.Trim().TrimEnd(';');
        var explainSql = trimmed.StartsWith("EXPLAIN", StringComparison.OrdinalIgnoreCase)
            ? statement
            : $"EXPLAIN QUERY PLAN {trimmed};";
        return ExecuteSqlCoreAsync(explainSql, recordHistory: false);
    }

    private async Task ExecuteSqlCoreAsync(string sql, bool recordHistory)
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(DatabasePath) || string.IsNullOrWhiteSpace(sql))
        {
            StatusMessage = "请先选择数据库并输入 SQL。";
            return;
        }

        if (!AllowWriteMode && !SqliteStatementClassifier.IsReadOnly(sql))
        {
            ClearResults();
            StatusMessage = "当前查询模式为只读。请切换到可写模式后再执行会修改数据库的 SQL。";
            return;
        }

        if (AllowWriteMode &&
            SqliteStatementClassifier.IsHighRisk(sql) &&
            !SkipHighRiskConfirmThisSession)
        {
            var category = SqliteStatementClassifier.Classify(sql);
            var categoryLabel = SqliteStatementClassifier.GetCategoryDisplayName(category);
            if (!_dialogService.Confirm(
                    "确认执行高风险操作",
                    $"即将执行 {categoryLabel} 语句：\n\n{sql}\n\n此操作可能不可逆。确认执行吗？\n\n（可在查询页勾选「本会话不再确认高风险」以跳过后续提示）"))
            {
                StatusMessage = "已取消执行。";
                return;
            }
        }

        _executeCts?.Cancel();
        _executeCts?.Dispose();
        _executeCts = new CancellationTokenSource();
        var token = _executeCts.Token;

        var settings = _settingsStore.Current;
        TimeSpan? timeout = settings.QueryTimeoutSeconds > 0
            ? TimeSpan.FromSeconds(settings.QueryTimeoutSeconds)
            : null;
        if (timeout is not null)
        {
            _executeCts.CancelAfter(timeout.Value);
        }

        IsBusy = true;
        StatusMessage = "正在执行查询…";

        try
        {
            var result = await _queryService.ExecuteAsync(
                DatabasePath,
                new QueryExecutionRequest(
                    sql,
                    AllowWriteMode,
                    settings.QueryMaxResultRows,
                    timeout),
                token);

            ApplyResult(result);
            StatusMessage = $"{result.Message} ({result.Duration.TotalMilliseconds:F0} ms)";

            if (recordHistory)
            {
                await _historyStore.AddAsync(sql, DatabasePath, token);
                await RefreshHistoryAsync(token);
            }
        }
        catch (OperationCanceledException)
        {
            ClearResults();
            StatusMessage = "查询已取消或超时。";
        }
        catch (Exception exception)
        {
            ClearResults();
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
            _executeCts?.Dispose();
            _executeCts = null;
            NotifyQueryStateChanged();
        }
    }

    private string ResolveExecutableSql()
    {
        // Prefer explicit selection later; for now full editor text (backward compatible).
        return QueryText;
    }

    [RelayCommand(CanExecute = nameof(CanCancelQuery))]
    public void CancelQuery()
    {
        _executeCts?.Cancel();
        StatusMessage = "正在取消查询…";
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

        // 查询页仅导出当前结果集（受结果行数上限约束），不重跑全量 SQL。
        StatusMessage = IsResultTruncated
            ? $"已导出当前已加载结果到 {Path.GetFileName(exportPath)}（结果曾截断，非全量）。"
            : $"已导出当前已加载结果到 {Path.GetFileName(exportPath)}。";
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

    [RelayCommand]
    public void ToggleWriteMode()
    {
        if (!AllowWriteMode)
        {
            if (!_dialogService.Confirm(
                    "启用可写模式",
                    "启用后，查询页将使用可写连接执行 SQL。\n\n" +
                    "INSERT / UPDATE / DELETE 会直接写入数据库。\n" +
                    "DDL、VACUUM、写 PRAGMA 等高风险操作默认需要二次确认。\n\n" +
                    "确认启用可写模式吗？"))
            {
                StatusMessage = "已保持只读模式。";
                return;
            }

            AllowWriteMode = true;
            StatusMessage = "已启用可写模式。请谨慎执行会修改数据库的 SQL。";
            return;
        }

        if (!_dialogService.Confirm(
                "切回只读模式",
                "确认退出可写模式吗？\n退出后写入/DDL 将被拦截，本会话的「跳过高风险确认」标记也会清除。"))
        {
            StatusMessage = "仍保持可写模式。";
            return;
        }

        AllowWriteMode = false;
        SkipHighRiskConfirmThisSession = false;
        StatusMessage = ReadyStatusMessage;
    }

    [RelayCommand]
    public void ApplyHistoryEntry(QueryHistoryEntry? entry)
    {
        if (entry is null || string.IsNullOrWhiteSpace(entry.Sql))
        {
            return;
        }

        QueryText = entry.Sql;
        StatusMessage = "已载入历史 SQL。";
    }

    partial void OnSelectedHistoryEntryChanged(QueryHistoryEntry? value)
    {
        if (value is null)
        {
            return;
        }

        ApplyHistoryEntry(value);
        // Keep selection clear so same entry can be re-picked later.
        SelectedHistoryEntry = null;
    }

    partial void OnSelectedFavoriteEntryChanged(QueryHistoryEntry? value)
    {
        if (value is null)
        {
            return;
        }

        ApplyHistoryEntry(value);
        SelectedFavoriteEntry = null;
    }

    [RelayCommand]
    public async Task ToggleFavoriteAsync(QueryHistoryEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        await _historyStore.SetFavoriteAsync(entry.Id, !entry.IsFavorite);
        await RefreshHistoryAsync();
        StatusMessage = entry.IsFavorite ? "已取消收藏。" : "已加入收藏。";
    }

    [RelayCommand]
    public async Task FavoriteCurrentQueryAsync()
    {
        if (string.IsNullOrWhiteSpace(QueryText))
        {
            StatusMessage = "没有可收藏的 SQL。";
            return;
        }

        await _historyStore.AddAsync(QueryText, DatabasePath);
        var recent = await _historyStore.GetRecentAsync(10);
        var match = recent.FirstOrDefault(entry =>
            string.Equals(entry.Sql, QueryText.Trim(), StringComparison.Ordinal));
        if (match is not null)
        {
            await _historyStore.SetFavoriteAsync(match.Id, isFavorite: true);
        }

        await RefreshHistoryAsync();
        StatusMessage = "已将当前 SQL 加入收藏。";
    }

    [RelayCommand]
    public async Task RefreshHistoryAsync(CancellationToken cancellationToken = default)
    {
        var recent = await _historyStore.GetRecentAsync(30, cancellationToken);
        var favorites = await _historyStore.GetFavoritesAsync(cancellationToken);

        RecentHistory.Clear();
        foreach (var entry in recent)
        {
            RecentHistory.Add(entry);
        }

        Favorites.Clear();
        foreach (var entry in favorites)
        {
            Favorites.Add(entry);
        }
    }

    private bool CanExportResults() => HasResults && !IsBusy;

    private bool CanCancelQuery() => IsBusy;

    partial void OnAllowWriteModeChanged(bool value)
    {
        OnPropertyChanged(nameof(QueryAccessModeLabel));
        OnPropertyChanged(nameof(QueryAccessModeSummary));
        OnPropertyChanged(nameof(ToggleWriteModeText));
        OnPropertyChanged(nameof(ShowWriteWarning));
        OnPropertyChanged(nameof(WriteModeBannerText));
    }

    partial void OnSkipHighRiskConfirmThisSessionChanged(bool value)
    {
        OnPropertyChanged(nameof(QueryAccessModeSummary));
        OnPropertyChanged(nameof(WriteModeBannerText));
        if (value && AllowWriteMode)
        {
            StatusMessage = "本会话将跳过高风险 SQL 二次确认。";
        }
    }

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
        IsResultTruncated = result.IsTruncated;

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
        IsResultTruncated = false;
        NotifyQueryStateChanged();
    }

    private static string BuildSelectTemplate(string tableName) =>
        $"select * from {SqliteIdentifier.Quote(tableName)} limit 100;";

    private static string BuildCountTemplate(string tableName) =>
        $"select count(*) as total_rows from {SqliteIdentifier.Quote(tableName)};";

    private static string BuildSchemaTemplate(string tableName) =>
        $"pragma table_info({SqliteIdentifier.Quote(tableName)});";

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
        OnPropertyChanged(nameof(ShowTruncationBanner));
    }
}
