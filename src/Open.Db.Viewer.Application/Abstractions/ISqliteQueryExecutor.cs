using Open.Db.Viewer.Domain.Models;

namespace Open.Db.Viewer.Application.Abstractions;

public interface ISqliteQueryExecutor
{
    Task<QueryExecutionResult> ExecuteAsync(
        string filePath,
        string sql,
        CancellationToken cancellationToken = default);
}
