using FluentAssertions;
using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.Services;
using Open.Db.Viewer.Shell.ViewModels;

namespace Open.Db.Viewer.Shell.Tests.ViewModels;

public class HomeViewModelTests
{
    [Fact]
    public async Task OpenDatabaseAsync_ShouldSetSuccessMessage_WhenDatabaseOpens()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeViewModel(databaseEntryService, new FakeFileDialogService(@"C:\data\demo.db"));

        await viewModel.OpenDatabaseAsync();

        viewModel.StatusMessage.Should().Be("数据库已打开。");
        viewModel.RecentEntries.Should().ContainSingle();
        viewModel.RecentEntries[0].FilePath.Should().Be(@"C:\data\demo.db");
    }

    [Fact]
    public async Task OpenDatabaseAsync_ShouldSetFailureMessage_WhenDatabaseOpenFails()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(false));
        var viewModel = new HomeViewModel(databaseEntryService, new FakeFileDialogService(@"C:\data\missing.db"));

        await viewModel.OpenDatabaseAsync();

        viewModel.StatusMessage.Should().Be("未找到数据库文件。");
    }

    [Fact]
    public async Task OpenDatabaseAsync_ShouldKeepStatusMessage_WhenDialogIsCancelled()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeViewModel(databaseEntryService, new FakeFileDialogService(null));

        await viewModel.OpenDatabaseAsync();

        viewModel.StatusMessage.Should().Be(HomeViewModel.DefaultStatusMessage);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateRecentEntriesFromRepository()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        await repository.SaveRecentAsync(new DatabaseEntry(
            Guid.NewGuid(),
            "sample",
            @"C:\data\sample.db",
            DateTimeOffset.UtcNow,
            false));
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeViewModel(databaseEntryService, new FakeFileDialogService(null));

        await viewModel.LoadAsync();

        viewModel.RecentEntries.Should().ContainSingle();
        viewModel.RecentEntries[0].Name.Should().Be("sample");
        viewModel.RecentEntries[0].FilePath.Should().Be(@"C:\data\sample.db");
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulatePinnedEntriesFromRepository()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        await repository.SavePinnedAsync(new DatabaseEntry(
            Guid.NewGuid(),
            "northwind",
            @"C:\data\northwind.db",
            DateTimeOffset.UtcNow,
            false));
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeViewModel(databaseEntryService, new FakeFileDialogService(null));

        await viewModel.LoadAsync();

        viewModel.PinnedEntries.Should().ContainSingle();
        viewModel.PinnedEntries[0].Name.Should().Be("northwind");
    }

    [Fact]
    public async Task OpenRecentAsync_ShouldReopenSelectedEntryAndNotifyShell()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var entry = new DatabaseEntry(
            Guid.NewGuid(),
            "sample",
            @"C:\data\sample.db",
            DateTimeOffset.UtcNow,
            false);
        await repository.SaveRecentAsync(entry);
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeViewModel(databaseEntryService, new FakeFileDialogService(null));
        string? openedPath = null;
        viewModel.DatabaseOpenedAsync = (path, _) =>
        {
            openedPath = path;
            return Task.CompletedTask;
        };

        await viewModel.LoadAsync();
        await viewModel.OpenRecentAsync(entry);

        openedPath.Should().Be(@"C:\data\sample.db");
        viewModel.StatusMessage.Should().Be("数据库已打开。");
    }

    [Fact]
    public async Task TogglePinAsync_ShouldMoveEntryIntoPinnedEntries()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var entry = new DatabaseEntry(
            Guid.NewGuid(),
            "sample",
            @"C:\data\sample.db",
            DateTimeOffset.UtcNow,
            false);
        await repository.SaveRecentAsync(entry);
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeViewModel(databaseEntryService, new FakeFileDialogService(null));

        await viewModel.LoadAsync();
        await viewModel.TogglePinAsync(viewModel.RecentEntries[0]);

        viewModel.PinnedEntries.Should().ContainSingle();
        viewModel.PinnedEntries[0].FilePath.Should().Be(@"C:\data\sample.db");
    }

    [Fact]
    public async Task SearchText_ShouldFilterVisibleEntries()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        await repository.SaveRecentAsync(new DatabaseEntry(Guid.NewGuid(), "northwind", @"C:\data\northwind.db", DateTimeOffset.UtcNow, false));
        await repository.SaveRecentAsync(new DatabaseEntry(Guid.NewGuid(), "chinook", @"C:\data\chinook.db", DateTimeOffset.UtcNow.AddMinutes(-10), false));
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeViewModel(databaseEntryService, new FakeFileDialogService(null));

        await viewModel.LoadAsync();
        viewModel.SearchText = "north";

        viewModel.FilteredRecentEntries.Should().ContainSingle();
        viewModel.FilteredRecentEntries[0].Name.Should().Be("northwind");
    }

    [Fact]
    public async Task LoadAsync_ShouldExposeFirstRunState_WhenThereAreNoEntries()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeViewModel(databaseEntryService, new FakeFileDialogService(null));

        await viewModel.LoadAsync();

        viewModel.ShowFirstRunState.Should().BeTrue();
        viewModel.HasAnyEntries.Should().BeFalse();
    }

    [Fact]
    public async Task SearchText_ShouldExposeNoResultsState_WhenNothingMatches()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        await repository.SaveRecentAsync(new DatabaseEntry(Guid.NewGuid(), "northwind", @"C:\data\northwind.db", DateTimeOffset.UtcNow, false));
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeViewModel(databaseEntryService, new FakeFileDialogService(null));

        await viewModel.LoadAsync();
        viewModel.SearchText = "does-not-exist";

        viewModel.ShowNoResultsState.Should().BeTrue();
        viewModel.FilteredRecentEntries.Should().BeEmpty();
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        private readonly string? _filePath;

        public FakeFileDialogService(string? filePath)
        {
            _filePath = filePath;
        }

        public string? PickSqliteFile() => _filePath;

        public string? PickCsvSavePath(string suggestedFileName) => null;
    }

    private sealed class InMemoryDatabaseEntryRepository : IDatabaseEntryRepository
    {
        private readonly List<DatabaseEntry> _recentEntries = new();
        private readonly List<DatabaseEntry> _pinnedEntries = new();

        public Task<IReadOnlyList<DatabaseEntry>> GetRecentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseEntry>>(_recentEntries.ToArray());

        public Task<IReadOnlyList<DatabaseEntry>> GetPinnedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseEntry>>(_pinnedEntries.ToArray());

        public Task SaveRecentAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
        {
            _recentEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task SavePinnedAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
        {
            _pinnedEntries.Add(entry with { IsPinned = true });
            return Task.CompletedTask;
        }

        public Task RemovePinnedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _pinnedEntries.RemoveAll(entry => entry.Id == id);
            return Task.CompletedTask;
        }
    }
}
