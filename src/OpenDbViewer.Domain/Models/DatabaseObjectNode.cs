namespace OpenDbViewer.Domain.Models;

public sealed record DatabaseObjectNode(
    string Id,
    string Kind,
    string Name,
    string? ParentId = null,
    IReadOnlyList<DatabaseObjectNode>? Children = null);
