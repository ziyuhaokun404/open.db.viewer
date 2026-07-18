using Sqlite.Viewer.Application.Abstractions;
using Sqlite.Viewer.Domain.Models;

namespace Sqlite.Viewer.Application.Services;

public sealed class QueryService
{
    private readonly ISqliteQueryExecutor _queryExecutor;

    public QueryService(ISqliteQueryExecutor queryExecutor)
    {
        _queryExecutor = queryExecutor;
    }

    public Task<QueryExecutionResult> ExecuteAsync(
        string filePath,
        QueryExecutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return _queryExecutor.ExecuteAsync(
            filePath,
            request.Sql,
            request.AllowWrite,
            request.MaxResultRows,
            request.Timeout,
            cancellationToken);
    }
}
