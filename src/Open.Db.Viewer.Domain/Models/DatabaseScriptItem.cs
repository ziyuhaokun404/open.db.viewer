namespace Open.Db.Viewer.Domain.Models;

public sealed record DatabaseScriptItem(
    string Name,
    string Kind,
    string? Sql);
