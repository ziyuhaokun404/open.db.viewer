using System.Data.Common;

namespace Open.Db.Viewer.Application.Abstractions;

public interface ISqliteConnectionFactory
{
    Task<DbConnection> CreateAsync(
        string filePath,
        SqliteConnectionAccessMode accessMode = SqliteConnectionAccessMode.ReadOnly,
        CancellationToken cancellationToken = default);
}

public enum SqliteConnectionAccessMode
{
    ReadOnly,
    ReadWrite
}
