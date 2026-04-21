using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Domain.Models;

namespace Open.Db.Viewer.Application.Services;

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
        return _queryExecutor.ExecuteAsync(filePath, request.Sql, cancellationToken);
    }
}
