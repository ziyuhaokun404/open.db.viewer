namespace Sqlite.Viewer.Domain.Models;

public sealed record DatabaseObjectNode(
    string Id,
    string Kind,
    string Name,
    string? ParentId = null,
    string? ParentObjectName = null,
    string? Sql = null,
    IReadOnlyList<DatabaseObjectNode>? Children = null)
{
    public string KindLabel => DatabaseObjectKinds.GetDisplayLabel(Kind);

    public string Subtitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ParentObjectName) &&
                (Kind.Equals(DatabaseObjectKinds.Index, StringComparison.OrdinalIgnoreCase) ||
                 Kind.Equals(DatabaseObjectKinds.Trigger, StringComparison.OrdinalIgnoreCase)))
            {
                return $"{KindLabel} · {ParentObjectName}";
            }

            return KindLabel;
        }
    }

    public bool IsGroup => Kind.Equals(DatabaseObjectKinds.Group, StringComparison.OrdinalIgnoreCase);

    public bool SupportsDataBrowse => DatabaseObjectKinds.IsBrowsableDataSource(Kind);
}
