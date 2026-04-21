using System.Data.Common;

namespace Open.Db.Viewer.Application.Abstractions;

public interface ISqliteConnectionFactory
{
    Task<DbConnection> CreateAsync(string filePath, CancellationToken cancellationToken = default);
}
