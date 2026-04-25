namespace Open.Db.Viewer.Domain.Models;

public sealed record TableMetadata(
    long RowCount,
    int PageSizeBytes,
    string SqliteVersion,
    string Encoding,
    int UserVersion,
    string? CreateSql,
    IReadOnlyList<DatabaseScriptItem> Indexes,
    IReadOnlyList<DatabaseScriptItem> Triggers);
