using FluentAssertions;

using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.Shell.ViewModels.Navigation;
using Open.Db.Viewer.ShellHost.Services;
using Open.Db.Viewer.ShellHost.ViewModels.Shell;

namespace Open.Db.Viewer.Shell.Tests.ViewModels;

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
        shell.NavigationItems.Single(item => item.Section == ShellSection.Settings).IsSelected.Should().BeTrue();
    }

    [Fact]
    public async Task OpenDatabaseAsync_ShouldNavigateToWorkspaceAndCaptureSession()
    {
        var shell = CreateShell(@"C:\data\demo.db");

        await shell.OpenDatabaseAsync();

        shell.CurrentSection.Should().Be(ShellSection.Workspace);
        shell.CurrentDatabasePath.Should().Be(@"C:\data\demo.db");
        shell.CurrentContentViewModel.Should().BeSameAs(shell.WorkspaceHost);
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
            new SettingsViewModel(),
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
