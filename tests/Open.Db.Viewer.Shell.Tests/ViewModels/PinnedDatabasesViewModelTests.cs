using FluentAssertions;
using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.ViewModels.Navigation;

namespace Open.Db.Viewer.Shell.Tests.ViewModels;

public class PinnedDatabasesViewModelTests
{
    [Fact]
    public async Task TogglePinAsync_ShouldRemovePinnedEntryFromCollection()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        await repository.SavePinnedAsync(new DatabaseEntry(Guid.NewGuid(), "app", @"C:\data\app.db", DateTimeOffset.UtcNow, false));
        var service = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new PinnedDatabasesViewModel(service);

        await viewModel.LoadAsync();
        await viewModel.TogglePinAsync(viewModel.FilteredEntries[0]);

        viewModel.FilteredEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchText_ShouldFilterPinnedEntriesOnly()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        await repository.SavePinnedAsync(new DatabaseEntry(Guid.NewGuid(), "app", @"C:\data\app.db", DateTimeOffset.UtcNow, false));
        await repository.SavePinnedAsync(new DatabaseEntry(Guid.NewGuid(), "northwind", @"C:\data\northwind.db", DateTimeOffset.UtcNow.AddMinutes(-5), false));
        var service = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new PinnedDatabasesViewModel(service);

        await viewModel.LoadAsync();
        viewModel.SearchText = "north";

        viewModel.FilteredEntries.Select(item => item.Name).Should().Equal("northwind");
    }

    private sealed class InMemoryDatabaseEntryRepository : IDatabaseEntryRepository
    {
        private readonly List<DatabaseEntry> _pinnedEntries = [];

        public Task<IReadOnlyList<DatabaseEntry>> GetRecentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseEntry>>(Array.Empty<DatabaseEntry>());

        public Task<IReadOnlyList<DatabaseEntry>> GetPinnedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseEntry>>(_pinnedEntries.ToArray());

        public Task SaveRecentAsync(DatabaseEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SavePinnedAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
        {
            _pinnedEntries.RemoveAll(item => item.FilePath.Equals(entry.FilePath, StringComparison.OrdinalIgnoreCase));
            _pinnedEntries.Add(entry with { IsPinned = true });
            return Task.CompletedTask;
        }

        public Task RemovePinnedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _pinnedEntries.RemoveAll(item => item.Id == id);
            return Task.CompletedTask;
        }
    }
}
