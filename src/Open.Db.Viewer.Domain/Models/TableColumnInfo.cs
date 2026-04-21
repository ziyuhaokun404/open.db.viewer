namespace Open.Db.Viewer.Domain.Models;

public sealed record TableColumnInfo(
    string Name,
    string DataType,
    bool IsNullable,
    string? DefaultValue,
    bool IsPrimaryKey);
