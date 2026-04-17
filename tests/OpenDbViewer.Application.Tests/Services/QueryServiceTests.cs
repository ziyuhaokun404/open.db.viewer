using FluentAssertions;
using OpenDbViewer.Application.Abstractions;
using OpenDbViewer.Application.Services;
using OpenDbViewer.Domain.Models;

namespace OpenDbViewer.Application.Tests.Services;

public class QueryServiceTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsTheFilePathAndSql_ToTheExecutor()
    {
        var executor = new RecordingQueryExecutor();
        var service = new QueryService(executor);
        var request = new QueryExecutionRequest("select 1");

        var result = await service.ExecuteAsync(@"C:\data\sample.db", request);

        executor.FilePath.Should().Be(@"C:\data\sample.db");
        executor.Sql.Should().Be("select 1");
        result.Columns.Should().ContainSingle().Which.Should().Be("value");
    }

    private sealed class RecordingQueryExecutor : ISqliteQueryExecutor
    {
        public string? FilePath { get; private set; }
        public string? Sql { get; private set; }

        public Task<QueryExecutionResult> ExecuteAsync(string filePath, string sql, CancellationToken cancellationToken = default)
        {
            FilePath = filePath;
            Sql = sql;

            return Task.FromResult(new QueryExecutionResult(
                new[] { "value" },
                new[] { new object?[] { 1 } },
                0,
                TimeSpan.FromMilliseconds(1),
                "ok"));
        }
    }
}
