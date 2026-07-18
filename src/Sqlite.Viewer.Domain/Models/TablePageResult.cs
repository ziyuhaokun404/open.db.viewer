namespace Sqlite.Viewer.Domain.Models;

public sealed record TablePageResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int PageNumber,
    int PageSize,
    bool HasNextPage,
    string? SortColumn,
    string? SortDirection,
    long TotalRowCount = 0)
{
    public int TotalPages => PageSize <= 0
        ? 0
        : (int)Math.Max(1, (TotalRowCount + PageSize - 1) / PageSize);

    public string PageRangeSummary
    {
        get
        {
            if (PageNumber <= 0 || Rows.Count == 0)
            {
                return TotalRowCount <= 0 ? "无数据" : "当前页无行";
            }

            var start = ((PageNumber - 1) * (long)PageSize) + 1;
            var end = start + Rows.Count - 1;
            return $"{start:N0}–{end:N0} / 共 {TotalRowCount:N0} 行";
        }
    }
}
