using System.Data.Common;
using Microsoft.Data.Sqlite;
using Open.Db.Viewer.Application.Abstractions;

namespace Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;

public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    public Task<DbConnection> CreateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = filePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        };

        DbConnection connection = new SqliteConnection(builder.ConnectionString);
        return Task.FromResult(connection);
    }
}
