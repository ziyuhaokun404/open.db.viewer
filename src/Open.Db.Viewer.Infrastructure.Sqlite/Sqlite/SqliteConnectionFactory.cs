using Microsoft.Data.Sqlite;
using Open.Db.Viewer.Application.Abstractions;
using System.Data.Common;

namespace Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;

public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    public Task<DbConnection> CreateAsync(string filePath, CancellationToken cancellationToken = default)
        => CreateAsync(filePath, SqliteConnectionAccessMode.ReadOnly, cancellationToken);

    public Task<DbConnection> CreateAsync(
        string filePath,
        SqliteConnectionAccessMode accessMode = SqliteConnectionAccessMode.ReadOnly,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = filePath,
            Mode = accessMode == SqliteConnectionAccessMode.ReadWrite
                ? SqliteOpenMode.ReadWrite
                : SqliteOpenMode.ReadOnly,
            Pooling = false
        };

        DbConnection connection = new SqliteConnection(builder.ConnectionString);
        return Task.FromResult(connection);
    }
}
