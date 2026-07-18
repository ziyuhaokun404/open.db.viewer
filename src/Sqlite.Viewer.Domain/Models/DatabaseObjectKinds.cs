namespace Sqlite.Viewer.Domain.Models;

public static class DatabaseObjectKinds
{
    public const string Group = "group";
    public const string Table = "table";
    public const string View = "view";
    public const string Index = "index";
    public const string Trigger = "trigger";
    public const string System = "system";

    public static string GetDisplayLabel(string kind) => kind.ToLowerInvariant() switch
    {
        Table => "表",
        View => "视图",
        Index => "索引",
        Trigger => "触发器",
        System => "系统表",
        Group => "分组",
        _ => kind
    };

    public static bool IsBrowsableDataSource(string kind) =>
        kind.Equals(Table, StringComparison.OrdinalIgnoreCase) ||
        kind.Equals(View, StringComparison.OrdinalIgnoreCase) ||
        kind.Equals(System, StringComparison.OrdinalIgnoreCase);

    public static bool IsSchemaLoadable(string kind) =>
        IsBrowsableDataSource(kind) ||
        kind.Equals(Index, StringComparison.OrdinalIgnoreCase) ||
        kind.Equals(Trigger, StringComparison.OrdinalIgnoreCase);
}
