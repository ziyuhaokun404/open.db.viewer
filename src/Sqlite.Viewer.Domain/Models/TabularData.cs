namespace Sqlite.Viewer.Domain.Models;

public sealed record TabularData(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows);
