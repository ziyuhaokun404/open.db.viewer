using OpenDbViewer.Application.Abstractions;
using OpenDbViewer.Domain.Models;

namespace OpenDbViewer.Application.Tests.Support;

public sealed class InMemoryDatabaseEntryRepository : IDatabaseEntryRepository
{
    private readonly List<DatabaseEntry> _recentEntries = new();
    private readonly List<DatabaseEntry> _pinnedEntries = new();

    public Task<IReadOnlyList<DatabaseEntry>> GetRecentAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DatabaseEntry>>(_recentEntries.ToArray());

    public Task<IReadOnlyList<DatabaseEntry>> GetPinnedAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DatabaseEntry>>(_pinnedEntries.ToArray());

    public Task SaveRecentAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
    {
        var existingIndex = _recentEntries.FindIndex(x => x.FilePath.Equals(entry.FilePath, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            _recentEntries[existingIndex] = entry;
        }
        else
        {
            _recentEntries.Add(entry);
        }

        return Task.CompletedTask;
    }

    public Task SavePinnedAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
    {
        var pinnedEntry = entry with { IsPinned = true };
        var existingIndex = _pinnedEntries.FindIndex(x => x.FilePath.Equals(entry.FilePath, StringComparison.OrdinalIgnoreCase));

        if (existingIndex >= 0)
        {
            _pinnedEntries[existingIndex] = pinnedEntry;
        }
        else
        {
            _pinnedEntries.Add(pinnedEntry);
        }

        return Task.CompletedTask;
    }

    public Task RemovePinnedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _pinnedEntries.RemoveAll(entry => entry.Id == id);
        return Task.CompletedTask;
    }
}
