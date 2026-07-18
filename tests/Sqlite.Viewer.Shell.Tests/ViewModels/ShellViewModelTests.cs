using FluentAssertions;

using Sqlite.Viewer.Application.Abstractions;
using Sqlite.Viewer.Application.Services;
using Sqlite.Viewer.Domain.Models;
using Sqlite.Viewer.Shell.ViewModels;
using Sqlite.Viewer.Shell.ViewModels.Navigation;
using Sqlite.Viewer.ShellHost.Services;
using Sqlite.Viewer.ShellHost.ViewModels.Navigation;
using Sqlite.Viewer.ShellHost.ViewModels.Shell;

namespace Sqlite.Viewer.Shell.Tests.ViewModels;

public class ShellViewModelTests
{
    [Fact]
    public void Constructor_ShouldDefaultToHomeSection()
    {
        var shell = CreateShell();

        shell.CurrentSection.Should().Be(ShellSection.Home);
        shell.NavigationItems.Select(item => item.Section)
            .Should().ContainInOrder(
                ShellSection.Home,
                ShellSection.Workspace);
    }

    [Fact]
    public void NavigateToSection_ShouldSwitchCurrentSection()
    {
        var shell = CreateShell();

        shell.NavigateToSection(ShellSection.Settings);

        shell.CurrentSection.Should().Be(ShellSection.Settings);
        shell.CurrentContentViewModel.Should().BeSameAs(shell.SettingsPage);
        shell.NavigationItems.Should().OnlyContain(item => !item.IsSelected);
    }

    [Fact]
    public async Task OpenDatabaseAsync_ShouldNavigateToWorkspaceAndCaptureSession()
    {
        var shell = CreateShell(@"C:\data\demo.db");

        await shell.OpenDatabaseAsync();

        shell.CurrentSection.Should().Be(ShellSection.Workspace);
        shell.CurrentDatabasePath.Should().Be(@"C:\data\demo.db");
        shell.CurrentContentViewModel.Should().BeSameAs(shell.Workspace);
    }

    [Fact]
    public async Task OpenDatabaseAsync_ShouldStayOnHomeWhenOpenFails()
    {
        var shell = CreateShell(@"C:\data\missing.db", databaseExists: false);

        await shell.OpenDatabaseAsync();

        shell.CurrentSection.Should().Be(ShellSection.Home);
        shell.CurrentDatabasePath.Should().BeEmpty();
        shell.CurrentContentViewModel.Should().BeSameAs(shell.HomeLanding);
    }

    [Fact]
    public async Task ReturnHomeAsync_ShouldNavigateBackToHomeSection()
    {
        var shell = CreateShell(@"C:\data\demo.db");

        await shell.OpenDatabaseAsync();
        await shell.Workspace.ReturnHomeAsync();

        shell.CurrentSection.Should().Be(ShellSection.Home);
        shell.CurrentContentViewModel.Should().BeSameAs(shell.HomeLanding);
    }

    [Fact]
    public async Task NavigateToSection_ShouldLoadHomePageOnDemand()
    {
        var shell = CreateShell();

        shell.NavigateToSection(ShellSection.Home);
        await shell.LoadCurrentSectionAsync();

        shell.HomeLanding.QuickOpenEntry.Should().BeNull();
        shell.CurrentContentViewModel.Should().BeSameAs(shell.HomeLanding);
    }

    [Fact]
    public async Task OpenDatabaseAsync_ShouldRefreshHomeSummariesAfterSuccess()
    {
        var shell = CreateShell(@"C:\data\demo.db");

        await shell.OpenDatabaseAsync();

        shell.HomeLanding.QuickOpenEntry.Should().NotBeNull();
    }

    private static ShellViewModel CreateShell(string? filePath = null, bool databaseExists = true)
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(databaseExists));
        var workspace = new FakeDatabaseWorkspaceViewModel();
        return new ShellViewModel(
            workspace,
            new HomeLandingViewModel(databaseEntryService, new FakeFileDialogService(filePath)),
            new SettingsViewModel(new ThemeService(), new Support.InMemoryAppSettingsStore()),
            new AboutViewModel());
    }

    private sealed class FakeFileDialogService(string? filePath) : IFileDialogService
    {
        public string? PickSqliteFile() => filePath;

        public string? PickCsvSavePath(string suggestedFileName) => null;
    }

    private sealed class FakeDatabaseWorkspaceViewModel : DatabaseWorkspaceViewModel
    {
        public string? LoadedPath { get; private set; }

        public FakeDatabaseWorkspaceViewModel()
            : base(new ObjectExplorerViewModel(), new SchemaViewModel(), new DataViewModel(), new QueryViewModel(
                new QueryService(new NoopSqliteQueryExecutor()),
                new ExportService(new NoopCsvExportWriter()),
                new FakeFileDialogService(null),
                new Support.NoopDialogService(), new Support.InMemoryAppSettingsStore(), new Support.InMemoryQueryHistoryStore()))
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
        public Task<QueryExecutionResult> ExecuteAsync(
            string filePath,
            string sql,
            bool allowWrite = false,
            int? maxResultRows = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new QueryExecutionResult(Array.Empty<string>(), Array.Empty<IReadOnlyList<object?>>(), 0, TimeSpan.Zero, string.Empty));
    }

    private sealed class NoopCsvExportWriter : ICsvExportWriter
    {
        public Task WriteAsync(string filePath, IReadOnlyList<string> columns, IReadOnlyList<IReadOnlyList<object?>> rows, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task WriteStreamingAsync(
            string filePath,
            IReadOnlyList<string> columns,
            IAsyncEnumerable<IReadOnlyList<object?>> rows,
            IProgress<long>? rowsWrittenProgress = null,
            CancellationToken cancellationToken = default)
        {
            await foreach (var _ in rows.WithCancellation(cancellationToken))
            {
            }
        }
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
