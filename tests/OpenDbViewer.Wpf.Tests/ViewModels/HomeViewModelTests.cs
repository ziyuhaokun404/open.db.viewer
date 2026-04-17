using FluentAssertions;
using OpenDbViewer.Application.Abstractions;
using OpenDbViewer.Application.Services;
using OpenDbViewer.Domain.Models;
using OpenDbViewer.Shell.Services;
using OpenDbViewer.Shell.ViewModels;

namespace OpenDbViewer.Wpf.Tests.ViewModels;

public class HomeViewModelTests
{
    [Fact]
    public async Task OpenDatabaseAsync_ShouldSetSuccessMessage_WhenDatabaseOpens()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new HomeViewModel(databaseEntryService, new FakeFileDialogService(@"C:\data\demo.db"));

        await viewModel.OpenDatabaseAsync();

        viewModel.StatusMessage.Should().Be("Database opened.");
    }

    [Fact]
    public async Task OpenDatabaseAsync_ShouldSetFailureMessage_WhenDatabaseOpenFails()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(false));
        var viewModel = new HomeViewModel(databaseEntryService, new FakeFileDialogService(@"C:\data\missing.db"));

        await viewModel.OpenDatabaseAsync();

        viewModel.StatusMessage.Should().Be("Database file was not found.");
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
