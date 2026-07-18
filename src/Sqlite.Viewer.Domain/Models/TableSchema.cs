namespace Sqlite.Viewer.Domain.Models;

public sealed record TableSchema(
    string TableName,
    IReadOnlyList<TableColumnInfo> Columns);
