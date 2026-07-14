using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Domain.Models;

namespace Open.Db.Viewer.Shell.Tests.Support;

public sealed class InMemoryQueryHistoryStore : IQueryHistoryStore
{
    private readonly List<QueryHistoryEntry> _entries = new();

    public Task<IReadOnlyList<QueryHistoryEntry>> GetRecentAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<QueryHistoryEntry>>(
            _entries.OrderByDescending(e => e.ExecutedAt).Take(take).ToArray());
    }

    public Task<IReadOnlyList<QueryHistoryEntry>> GetFavoritesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<QueryHistoryEntry>>(
            _entries.Where(e => e.IsFavorite).OrderByDescending(e => e.ExecutedAt).ToArray());
    }

    public Task AddAsync(string sql, string? databasePath = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return Task.CompletedTask;
        }

        _entries.Insert(0, new QueryHistoryEntry(Guid.NewGuid(), sql.Trim(), DateTimeOffset.UtcNow, false, databasePath));
        return Task.CompletedTask;
    }

    public Task SetFavoriteAsync(Guid id, bool isFavorite, CancellationToken cancellationToken = default)
    {
        var index = _entries.FindIndex(e => e.Id == id);
        if (index >= 0)
        {
            _entries[index] = _entries[index] with { IsFavorite = isFavorite };
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _entries.RemoveAll(e => e.Id == id);
        return Task.CompletedTask;
    }
}
