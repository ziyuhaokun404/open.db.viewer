using OpenDbViewer.Domain.Models;

namespace OpenDbViewer.Application.Abstractions;

public interface ISqliteQueryExecutor
{
    Task<QueryExecutionResult> ExecuteAsync(
        string filePath,
        string sql,
        CancellationToken cancellationToken = default);
}
