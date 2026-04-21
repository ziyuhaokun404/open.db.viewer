namespace Open.Db.Viewer.Domain.Models;

public sealed record QueryExecutionResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int AffectedRows,
    TimeSpan Duration,
    string Message);
