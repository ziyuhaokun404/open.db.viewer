namespace OpenDbViewer.Domain.Models;

public sealed record TableSchema(
    string TableName,
    IReadOnlyList<TableColumnInfo> Columns);
