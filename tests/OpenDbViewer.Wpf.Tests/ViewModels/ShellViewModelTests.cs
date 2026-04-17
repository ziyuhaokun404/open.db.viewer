using FluentAssertions;
using OpenDbViewer.Application.Abstractions;
using OpenDbViewer.Application.Services;
using OpenDbViewer.Domain.Models;
using OpenDbViewer.Shell.Services;
using OpenDbViewer.Shell.ViewModels;

namespace OpenDbViewer.Wpf.Tests.ViewModels;

public class ShellViewModelTests
{
    [Fact]
    public async Task OpenDatabaseAsync_ShouldNavigateToWorkspaceAfterSuccessfulOpen()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var workspace = new FakeDatabaseWorkspaceViewModel();
        var home = new HomeViewModel(databaseEntryService, new FakeFileDialogService(@"C:\data\demo.db"));
        var shell = new ShellViewModel(home, workspace);

        await home.OpenDatabaseAsync();

        shell.CurrentPage.Should().BeSameAs(workspace);
        workspace.LoadedPath.Should().Be(@"C:\data\demo.db");
    }

    [Fact]
    public async Task OpenDatabaseAsync_ShouldStayOnHomeWhenOpenFails()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(false));
        var workspace = new FakeDatabaseWorkspaceViewModel();
        var home = new HomeViewModel(databaseEntryService, new FakeFileDialogService(@"C:\data\missing.db"));
        var shell = new ShellViewModel(home, workspace);

        await home.OpenDatabaseAsync();

        shell.CurrentPage.Should().BeSameAs(home);
        workspace.LoadedPath.Should().BeNull();
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

    private sealed class FakeDatabaseWorkspaceViewModel : DatabaseWorkspaceViewModel
    {
        public string? LoadedPath { get; private set; }

        public FakeDatabaseWorkspaceViewModel()
            : base(new ObjectExplorerViewModel(), new SchemaViewModel(), new DataViewModel(), new QueryViewModel(
                new QueryService(new NoopSqliteQueryExecutor()),
                new ExportService(new NoopCsvExportWriter()),
                new FakeFileDialogService(null)))
        {
        }

        public override Task LoadAsync(string databasePath, CancellationToken cancellationToken = default)
        {
            LoadedPath = databasePath;
            return Task.CompletedTask;
        }
    }

    private sealed class NoopSqliteQueryExecutor : ISqliteQueryExecutor
    {
        public Task<QueryExecutionResult> ExecuteAsync(string filePath, string sql, CancellationToken cancellationToken = default) =>
            Task.FromResult(new QueryExecutionResult(Array.Empty<string>(), Array.Empty<IReadOnlyList<object?>>(), 0, TimeSpan.Zero, string.Empty));
    }

    private sealed class NoopCsvExportWriter : ICsvExportWriter
    {
        public Task WriteAsync(string filePath, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<object?>> rows, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
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
