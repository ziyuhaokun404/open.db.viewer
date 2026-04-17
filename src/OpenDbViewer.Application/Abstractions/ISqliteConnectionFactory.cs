using System.Data.Common;

namespace OpenDbViewer.Application.Abstractions;

public interface ISqliteConnectionFactory
{
    Task<DbConnection> CreateAsync(string filePath, CancellationToken cancellationToken = default);
}
