using FluentAssertions;
using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.Services;
using Open.Db.Viewer.Shell.ViewModels.Navigation;

namespace Open.Db.Viewer.Shell.Tests.ViewModels;

public class HomeLandingViewModelTests
{
    [Fact]
    public async Task LoadAsync_ShouldExposeQuickOpenAndSummaryCollections()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        await repository.SaveRecentAsync(new DatabaseEntry(Guid.NewGuid(), "app", @"C:\data\app.db", DateTimeOffset.UtcNow, false));
        await repository.SavePinnedAsync(new DatabaseEntry(Guid.NewGuid(), "northwind", @"C:\data\northwind.db", DateTimeOffset.UtcNow.AddMinutes(-10), false));
        var service = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeLandingViewModel(service, new FakeFileDialogService(null));

        await viewModel.LoadAsync();

        viewModel.QuickOpenEntry.Should().NotBeNull();
        viewModel.QuickOpenEntry!.Name.Should().Be("app");
        viewModel.RecentSummary.Should().ContainSingle();
        viewModel.PinnedSummary.Should().ContainSingle();
    }

    [Fact]
    public async Task OpenQuickOpenAsync_ShouldOpenRecentEntryAndNotifyShell()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        await repository.SaveRecentAsync(new DatabaseEntry(Guid.NewGuid(), "app", @"C:\data\app.db", DateTimeOffset.UtcNow, false));
        var service = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeLandingViewModel(service, new FakeFileDialogService(null));
        string? openedPath = null;
        viewModel.DatabaseOpenedAsync = (path, _) =>
        {
            openedPath = path;
            return Task.CompletedTask;
        };

        await viewModel.LoadAsync();
        await viewModel.OpenQuickOpenAsync();

        openedPath.Should().Be(@"C:\data\app.db");
    }

    [Fact]
    public async Task OpenEntryAsync_ShouldOpenPinnedOrRecentSummaryEntry_AndNotifyShell()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var entry = new DatabaseEntry(Guid.NewGuid(), "app", @"C:\data\app.db", DateTimeOffset.UtcNow, false);
        await repository.SaveRecentAsync(entry);
        var service = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeLandingViewModel(service, new FakeFileDialogService(null));
        string? openedPath = null;
        viewModel.DatabaseOpenedAsync = (path, _) =>
        {
            openedPath = path;
            return Task.CompletedTask;
        };

        await viewModel.OpenEntryAsync(entry);

        openedPath.Should().Be(@"C:\data\app.db");
    }

    [Fact]
    public async Task TogglePinAsync_ShouldRemovePinnedSummaryEntry_WhenEntryIsAlreadyPinned()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var entry = new DatabaseEntry(Guid.NewGuid(), "app", @"C:\data\app.db", DateTimeOffset.UtcNow, true);
        await repository.SavePinnedAsync(entry);
        var service = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeLandingViewModel(service, new FakeFileDialogService(null));

        await viewModel.LoadAsync();
        await viewModel.TogglePinAsync(viewModel.PinnedSummary[0]);

        viewModel.PinnedSummary.Should().BeEmpty();
    }

    private sealed class FakeFileDialogService(string? filePath) : IFileDialogService
    {
        public string? PickSqliteFile() => filePath;

        public string? PickCsvSavePath(string suggestedFileName) => null;
    }

    private sealed class InMemoryDatabaseEntryRepository : IDatabaseEntryRepository
    {
        private readonly List<DatabaseEntry> _recentEntries = [];
        private readonly List<DatabaseEntry> _pinnedEntries = [];

        public Task<IReadOnlyList<DatabaseEntry>> GetRecentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseEntry>>(_recentEntries.ToArray());

        public Task<IReadOnlyList<DatabaseEntry>> GetPinnedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseEntry>>(_pinnedEntries.ToArray());

        public Task SaveRecentAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
        {
            _recentEntries.RemoveAll(item => item.FilePath.Equals(entry.FilePath, StringComparison.OrdinalIgnoreCase));
            _recentEntries.Add(entry);
            return Task.CompletedTask;
        }

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
