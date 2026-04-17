namespace OpenDbViewer.Domain.Models;

public sealed record TablePageResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int PageNumber,
    int PageSize,
    bool HasNextPage,
    string? SortColumn,
    string? SortDirection);
