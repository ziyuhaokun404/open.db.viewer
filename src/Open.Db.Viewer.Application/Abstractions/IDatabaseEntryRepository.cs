using Open.Db.Viewer.Domain.Models;

namespace Open.Db.Viewer.Application.Abstractions;

public interface IDatabaseEntryRepository
{
    Task<IReadOnlyList<DatabaseEntry>> GetRecentAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DatabaseEntry>> GetPinnedAsync(CancellationToken cancellationToken = default);

    Task SaveRecentAsync(DatabaseEntry entry, CancellationToken cancellationToken = default);

    Task SavePinnedAsync(DatabaseEntry entry, CancellationToken cancellationToken = default);

    Task RemovePinnedAsync(Guid id, CancellationToken cancellationToken = default);
}
