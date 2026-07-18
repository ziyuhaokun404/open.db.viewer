namespace Sqlite.Viewer.Domain.Models;

/// <summary>
/// 用户可持久化设置。写入 %LocalAppData%\SqliteViewer\settings.json。
/// </summary>
public sealed class AppSettings
{
    public const int DefaultPageSize = 100;
    public const int DefaultQueryMaxResultRows = 5_000;
    public const int DefaultQueryTimeoutSeconds = 30;
    public const int MinQueryMaxResultRows = 100;
    public const int MaxQueryMaxResultRows = 100_000;
    public const int MinQueryTimeoutSeconds = 0;
    public const int MaxQueryTimeoutSeconds = 600;

    /// <summary>System | Light | Dark</summary>
    public string ThemePreference { get; set; } = "System";

    public int DefaultPageSizeValue { get; set; } = DefaultPageSize;

    /// <summary>查询结果最大返回行数；超出则截断并标记 IsTruncated。</summary>
    public int QueryMaxResultRows { get; set; } = DefaultQueryMaxResultRows;

    /// <summary>查询超时秒数；0 表示不超时（仍可手动取消）。</summary>
    public int QueryTimeoutSeconds { get; set; } = DefaultQueryTimeoutSeconds;

    public void Normalize()
    {
        if (ThemePreference is not ("System" or "Light" or "Dark"))
        {
            ThemePreference = "System";
        }

        if (DefaultPageSizeValue is not (50 or 100 or 200 or 500))
        {
            DefaultPageSizeValue = DefaultPageSize;
        }

        QueryMaxResultRows = Math.Clamp(QueryMaxResultRows, MinQueryMaxResultRows, MaxQueryMaxResultRows);
        QueryTimeoutSeconds = Math.Clamp(QueryTimeoutSeconds, MinQueryTimeoutSeconds, MaxQueryTimeoutSeconds);
    }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            ThemePreference = ThemePreference,
            DefaultPageSizeValue = DefaultPageSizeValue,
            QueryMaxResultRows = QueryMaxResultRows,
            QueryTimeoutSeconds = QueryTimeoutSeconds
        };
    }
}
