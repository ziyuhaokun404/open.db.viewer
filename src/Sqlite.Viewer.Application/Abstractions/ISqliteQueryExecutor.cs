using Sqlite.Viewer.Domain.Models;

namespace Sqlite.Viewer.Application.Abstractions;

public interface ISqliteQueryExecutor
{
    Task<QueryExecutionResult> ExecuteAsync(
        string filePath,
        string sql,
        bool allowWrite = false,
        int? maxResultRows = null,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}
