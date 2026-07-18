using System.Reflection;

namespace Sqlite.Viewer.ShellHost.ViewModels.Navigation;

public sealed class AboutViewModel
{
    public string Title => "关于";

    public string ProductName => "SQLite Viewer";

    public string ProductSubtitle => "安全优先的 SQLite 桌面查看器";

    public string VersionDisplay
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version is null ? "开发版" : $"v{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public string InformationalVersion
    {
        get
        {
            var attr = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return string.IsNullOrWhiteSpace(attr?.InformationalVersion)
                ? VersionDisplay
                : attr.InformationalVersion;
        }
    }

    public string FrameworkDisplay =>
        System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

    public string Description =>
        "SQLite Viewer 面向日常排查与分析场景：只读优先打开 SQLite 库，" +
        "支持对象浏览（表/视图/索引/触发器）、分页数据、SQL 查询、历史收藏与 CSV 导出。" +
        "可写模式需显式启用，并对 DDL / 维护 / 写 PRAGMA 等高风险语句二次确认。";

    public string ChangelogSummary =>
        """
        近期能力摘要
        • P0 查询行数上限、取消/超时、标识符转义、设置持久化
        • P1 完整对象目录、查询历史/收藏、当前语句、EXPLAIN
        • P2 跳页/筛选、NULL·BLOB 展示、复制、整表流式导出
        • P3 危险 SQL 策略与可写 UX、工作台拆分、CI 与发布说明
        """;

    public string KnownLimitations =>
        "当前仅支持 SQLite；不做网格单元格编辑；不做多数据库客户端。" +
        "查询导出仅为已加载结果（受结果行数上限约束）。";

    public string Copyright => $"© {DateTime.Now.Year} SQLite Viewer";
}
