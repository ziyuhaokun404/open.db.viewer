namespace Open.Db.Viewer.Domain.Models;

public sealed record QueryExecutionRequest(
    string Sql,
    bool AllowWrite = false,
    int? MaxResultRows = null,
    TimeSpan? Timeout = null);
