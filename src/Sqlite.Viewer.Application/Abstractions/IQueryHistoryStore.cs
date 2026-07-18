using Sqlite.Viewer.Domain.Models;

namespace Sqlite.Viewer.Application.Abstractions;

public interface IQueryHistoryStore
{
    Task<IReadOnlyList<QueryHistoryEntry>> GetRecentAsync(int take = 50, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QueryHistoryEntry>> GetFavoritesAsync(CancellationToken cancellationToken = default);

    Task AddAsync(string sql, string? databasePath = null, CancellationToken cancellationToken = default);

    Task SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
}
