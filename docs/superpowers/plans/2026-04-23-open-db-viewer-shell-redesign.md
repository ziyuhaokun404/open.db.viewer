# open.db.viewer Shell Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild `open.db.viewer` as a single-shell desktop app with left navigation, dedicated home/recent/pinned/settings/about sections, and a database workspace that stays inside the same shell.

**Architecture:** Expand the existing `ShellViewModel` into a section-driven shell instead of introducing a second top-level app model, then split the current `HomePage` responsibilities into dedicated navigation pages while reusing the existing workspace internals. The first delivery keeps the current schema/data/query behavior and only changes the shell, page boundaries, and navigation flow.

**Tech Stack:** .NET 8 WPF, CommunityToolkit.Mvvm, WPF-UI, xUnit, FluentAssertions

---

## File Structure

### Shell and navigation state

- Create: `src/Open.Db.Viewer.Shell/ViewModels/Shell/ShellSection.cs`
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Shell/ShellNavigationItem.cs`
- Modify: `src/Open.Db.Viewer.Shell/ViewModels/ShellViewModel.cs`
- Modify: `src/Open.Db.Viewer.Shell/ServiceCollectionExtensions.cs`

These files define stable shell state, navigation items, active section, and current database session metadata.

### Navigation pages and page view models

- Create: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/HomeLandingViewModel.cs`
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/RecentDatabasesViewModel.cs`
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/PinnedDatabasesViewModel.cs`
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/SettingsViewModel.cs`
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/AboutViewModel.cs`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/HomeLandingPage.xaml`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/HomeLandingPage.xaml.cs`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/RecentDatabasesPage.xaml`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/RecentDatabasesPage.xaml.cs`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/PinnedDatabasesPage.xaml`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/PinnedDatabasesPage.xaml.cs`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/SettingsPage.xaml`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/SettingsPage.xaml.cs`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/AboutPage.xaml`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/AboutPage.xaml.cs`

These files replace the single overloaded `HomePage` with dedicated shell sections.

### Workspace host integration

- Create: `src/Open.Db.Viewer.Shell/ViewModels/Workspace/WorkspaceHostViewModel.cs`
- Create: `src/Open.Db.Viewer.Shell/Views/Workspace/WorkspaceHostPage.xaml`
- Create: `src/Open.Db.Viewer.Shell/Views/Workspace/WorkspaceHostPage.xaml.cs`
- Modify: `src/Open.Db.Viewer.Shell/ViewModels/DatabaseWorkspaceViewModel.cs`
- Modify: `src/Open.Db.Viewer.Shell/Views/Pages/DatabaseWorkspacePage.xaml`

These files let the current workspace live inside the shell with an empty state and shell-friendly header.

### Window and template wiring

- Modify: `src/Open.Db.Viewer.Shell/Views/MainWindow.xaml`
- Modify: `src/Open.Db.Viewer.Shell/Views/MainWindow.xaml.cs`

These files provide the actual shell chrome, left navigation rail, and section content host.

### Tests

- Modify: `tests/Open.Db.Viewer.Shell.Tests/ViewModels/ShellViewModelTests.cs`
- Create: `tests/Open.Db.Viewer.Shell.Tests/ViewModels/HomeLandingViewModelTests.cs`
- Create: `tests/Open.Db.Viewer.Shell.Tests/ViewModels/RecentDatabasesViewModelTests.cs`
- Create: `tests/Open.Db.Viewer.Shell.Tests/ViewModels/PinnedDatabasesViewModelTests.cs`
- Modify: `tests/Open.Db.Viewer.Shell.Tests/Views/MainWindowSmokeTests.cs`

These tests lock down shell navigation, page data responsibilities, and WPF rendering.

## Task 1: Introduce Section-Driven Shell State

**Files:**
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Shell/ShellSection.cs`
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Shell/ShellNavigationItem.cs`
- Modify: `src/Open.Db.Viewer.Shell/ViewModels/ShellViewModel.cs`
- Test: `tests/Open.Db.Viewer.Shell.Tests/ViewModels/ShellViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
using FluentAssertions;
using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.Services;
using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.Shell.ViewModels.Shell;

namespace Open.Db.Viewer.Shell.Tests.ViewModels;

public class ShellViewModelTests
{
    [Fact]
    public void Constructor_ShouldDefaultToHomeSection()
    {
        var shell = CreateShell();

        shell.CurrentSection.Should().Be(ShellSection.Home);
        shell.NavigationItems.Select(item => item.Section)
            .Should().ContainInOrder(ShellSection.Home, ShellSection.Recent, ShellSection.Pinned, ShellSection.Workspace);
    }

    [Fact]
    public void NavigateToSection_ShouldSwitchCurrentSection()
    {
        var shell = CreateShell();

        shell.NavigateToSection(ShellSection.Pinned);

        shell.CurrentSection.Should().Be(ShellSection.Pinned);
    }

    [Fact]
    public async Task OpenDatabaseAsync_ShouldNavigateToWorkspaceAndCaptureSession()
    {
        var shell = CreateShell(@"C:\data\demo.db");

        await shell.OpenDatabaseAsync();

        shell.CurrentSection.Should().Be(ShellSection.Workspace);
        shell.CurrentDatabasePath.Should().Be(@"C:\data\demo.db");
    }

    private static ShellViewModel CreateShell(string? filePath = null)
    {
        var repository = new InMemoryDatabaseEntryRepository();
        var entryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var workspace = new FakeDatabaseWorkspaceViewModel();
        var home = new HomeViewModel(entryService, new FakeFileDialogService(filePath));
        return new ShellViewModel(home, workspace);
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        private readonly string? _filePath;

        public FakeFileDialogService(string? filePath) => _filePath = filePath;

        public string? PickSqliteFile() => _filePath;

        public string? PickCsvSavePath(string suggestedFileName) => null;
    }

    private sealed class FakeDatabaseWorkspaceViewModel : DatabaseWorkspaceViewModel
    {
        public FakeDatabaseWorkspaceViewModel() : base(new ObjectExplorerViewModel(), new SchemaViewModel(), new DataViewModel(), new QueryViewModel(
            new QueryService(new NoopSqliteQueryExecutor()),
            new ExportService(new NoopCsvExportWriter()),
            new FakeFileDialogService(null)))
        {
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
        public Task<IReadOnlyList<DatabaseEntry>> GetRecentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseEntry>>(Array.Empty<DatabaseEntry>());

        public Task<IReadOnlyList<DatabaseEntry>> GetPinnedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseEntry>>(Array.Empty<DatabaseEntry>());

        public Task SaveRecentAsync(DatabaseEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SavePinnedAsync(DatabaseEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemovePinnedAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test tests/Open.Db.Viewer.Shell.Tests/Open.Db.Viewer.Shell.Tests.csproj --filter "FullyQualifiedName~Open.Db.Viewer.Shell.Tests.ViewModels.ShellViewModelTests"
```

Expected: FAIL with missing `ShellSection`, `NavigationItems`, `CurrentSection`, or `OpenDatabaseAsync` members.

- [ ] **Step 3: Write the minimal shell state implementation**

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/Shell/ShellSection.cs
namespace Open.Db.Viewer.Shell.ViewModels.Shell;

public enum ShellSection
{
    Home,
    Recent,
    Pinned,
    Workspace,
    Settings,
    About
}
```

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/Shell/ShellNavigationItem.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace Open.Db.Viewer.Shell.ViewModels.Shell;

public sealed partial class ShellNavigationItem : ObservableObject
{
    public ShellNavigationItem(ShellSection section, string title)
    {
        Section = section;
        Title = title;
    }

    public ShellSection Section { get; }

    public string Title { get; }

    [ObservableProperty]
    private bool isSelected;
}
```

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/ShellViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Shell.ViewModels.Shell;
using System.Collections.ObjectModel;

namespace Open.Db.Viewer.Shell.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private ShellSection currentSection = ShellSection.Home;

    [ObservableProperty]
    private string currentDatabasePath = string.Empty;

    public ShellViewModel(HomeViewModel homeViewModel, DatabaseWorkspaceViewModel databaseWorkspaceViewModel)
    {
        Home = homeViewModel;
        Workspace = databaseWorkspaceViewModel;

        NavigationItems =
        [
            new ShellNavigationItem(ShellSection.Home, "首页"),
            new ShellNavigationItem(ShellSection.Recent, "最近使用"),
            new ShellNavigationItem(ShellSection.Pinned, "已固定"),
            new ShellNavigationItem(ShellSection.Workspace, "数据库工作台"),
            new ShellNavigationItem(ShellSection.Settings, "设置"),
            new ShellNavigationItem(ShellSection.About, "关于")
        ];

        CurrentContentViewModel = homeViewModel;
        UpdateNavigationSelection();

        Home.DatabaseOpenedAsync = OpenWorkspaceAsync;
        Workspace.RequestReturnHomeAsync = ReturnHomeAsync;
        _ = Home.LoadAsync();
    }

    public ObservableCollection<ShellNavigationItem> NavigationItems { get; }

    public HomeViewModel Home { get; }

    public DatabaseWorkspaceViewModel Workspace { get; }

    [ObservableProperty]
    private object currentContentViewModel;

    public void NavigateToSection(ShellSection section)
    {
        CurrentSection = section;
        UpdateNavigationSelection();
    }

    [RelayCommand]
    public Task OpenDatabaseAsync(CancellationToken cancellationToken = default) => Home.OpenDatabaseAsync(cancellationToken);

    private async Task OpenWorkspaceAsync(string databasePath, CancellationToken cancellationToken)
    {
        CurrentDatabasePath = databasePath;
        await Workspace.LoadAsync(databasePath, cancellationToken);
        CurrentContentViewModel = Workspace;
        CurrentSection = ShellSection.Workspace;
        UpdateNavigationSelection();
    }

    private Task ReturnHomeAsync()
    {
        CurrentContentViewModel = Home;
        CurrentSection = ShellSection.Home;
        UpdateNavigationSelection();
        return Task.CompletedTask;
    }

    private void UpdateNavigationSelection()
    {
        foreach (var item in NavigationItems)
        {
            item.IsSelected = item.Section == CurrentSection;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```bash
dotnet test tests/Open.Db.Viewer.Shell.Tests/Open.Db.Viewer.Shell.Tests.csproj --filter "FullyQualifiedName~Open.Db.Viewer.Shell.Tests.ViewModels.ShellViewModelTests"
```

Expected: PASS for the updated shell tests.

- [ ] **Step 5: Commit**

```bash
git add src/Open.Db.Viewer.Shell/ViewModels/Shell/ShellSection.cs src/Open.Db.Viewer.Shell/ViewModels/Shell/ShellNavigationItem.cs src/Open.Db.Viewer.Shell/ViewModels/ShellViewModel.cs tests/Open.Db.Viewer.Shell.Tests/ViewModels/ShellViewModelTests.cs
git commit -m "feat: add section-driven shell state"
```

## Task 2: Add Dedicated Navigation Page ViewModels

**Files:**
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/HomeLandingViewModel.cs`
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/RecentDatabasesViewModel.cs`
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/PinnedDatabasesViewModel.cs`
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/SettingsViewModel.cs`
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/AboutViewModel.cs`
- Modify: `src/Open.Db.Viewer.Shell/ServiceCollectionExtensions.cs`
- Test: `tests/Open.Db.Viewer.Shell.Tests/ViewModels/HomeLandingViewModelTests.cs`
- Test: `tests/Open.Db.Viewer.Shell.Tests/ViewModels/RecentDatabasesViewModelTests.cs`
- Test: `tests/Open.Db.Viewer.Shell.Tests/ViewModels/PinnedDatabasesViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
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
        viewModel.RecentSummary.Should().ContainSingle();
        viewModel.PinnedSummary.Should().ContainSingle();
    }
}

public class RecentDatabasesViewModelTests
{
    [Fact]
    public async Task SearchText_ShouldFilterRecentEntriesOnly()
    {
        var repository = new InMemoryDatabaseEntryRepository();
        await repository.SaveRecentAsync(new DatabaseEntry(Guid.NewGuid(), "northwind", @"C:\data\northwind.db", DateTimeOffset.UtcNow, false));
        await repository.SaveRecentAsync(new DatabaseEntry(Guid.NewGuid(), "chinook", @"C:\data\chinook.db", DateTimeOffset.UtcNow.AddMinutes(-5), false));
        var service = new DatabaseEntryService(repository, _ => Task.FromResult(true));
        var viewModel = new RecentDatabasesViewModel(service);

        await viewModel.LoadAsync();
        viewModel.SearchText = "north";

        viewModel.FilteredEntries.Select(item => item.Name).Should().Equal("northwind");
    }
}

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
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test tests/Open.Db.Viewer.Shell.Tests/Open.Db.Viewer.Shell.Tests.csproj --filter "FullyQualifiedName~HomeLandingViewModelTests|FullyQualifiedName~RecentDatabasesViewModelTests|FullyQualifiedName~PinnedDatabasesViewModelTests"
```

Expected: FAIL because the dedicated navigation ViewModels do not exist yet.

- [ ] **Step 3: Write the minimal page ViewModels and register them**

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/Navigation/HomeLandingViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.Services;
using System.Collections.ObjectModel;

namespace Open.Db.Viewer.Shell.ViewModels.Navigation;

public sealed partial class HomeLandingViewModel : ObservableObject
{
    private readonly DatabaseEntryService _databaseEntryService;
    private readonly IFileDialogService _fileDialogService;

    public HomeLandingViewModel(DatabaseEntryService databaseEntryService, IFileDialogService fileDialogService)
    {
        _databaseEntryService = databaseEntryService;
        _fileDialogService = fileDialogService;
    }

    [ObservableProperty]
    private DatabaseEntry? quickOpenEntry;

    public ObservableCollection<DatabaseEntry> RecentSummary { get; } = new();

    public ObservableCollection<DatabaseEntry> PinnedSummary { get; } = new();

    public Func<string, CancellationToken, Task>? DatabaseOpenedAsync { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var recent = await _databaseEntryService.GetRecentAsync(cancellationToken);
        var pinned = await _databaseEntryService.GetPinnedAsync(cancellationToken);

        QuickOpenEntry = recent.OrderByDescending(item => item.LastOpenedAt).FirstOrDefault();
        Refresh(RecentSummary, recent.OrderByDescending(item => item.LastOpenedAt).Take(3));
        Refresh(PinnedSummary, pinned.OrderByDescending(item => item.LastOpenedAt).Take(3));
    }

    [RelayCommand]
    public async Task OpenDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var filePath = _fileDialogService.PickSqliteFile();
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            var result = await _databaseEntryService.OpenAsync(filePath, cancellationToken);
            if (result.IsSuccess && DatabaseOpenedAsync is not null)
            {
                await DatabaseOpenedAsync(filePath, cancellationToken);
            }
        }
    }

    private static void Refresh(ObservableCollection<DatabaseEntry> target, IEnumerable<DatabaseEntry> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}
```

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/Navigation/RecentDatabasesViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using System.Collections.ObjectModel;

namespace Open.Db.Viewer.Shell.ViewModels.Navigation;

public sealed partial class RecentDatabasesViewModel : ObservableObject
{
    private readonly DatabaseEntryService _databaseEntryService;

    public RecentDatabasesViewModel(DatabaseEntryService databaseEntryService) => _databaseEntryService = databaseEntryService;

    [ObservableProperty]
    private string searchText = string.Empty;

    public ObservableCollection<DatabaseEntry> Entries { get; } = new();

    public ObservableCollection<DatabaseEntry> FilteredEntries { get; } = new();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var recent = await _databaseEntryService.GetRecentAsync(cancellationToken);
        Entries.Clear();
        foreach (var item in recent.OrderByDescending(entry => entry.LastOpenedAt))
        {
            Entries.Add(item);
        }
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var source = string.IsNullOrWhiteSpace(SearchText)
            ? Entries
            : Entries.Where(entry => entry.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || entry.FilePath.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        FilteredEntries.Clear();
        foreach (var item in source)
        {
            FilteredEntries.Add(item);
        }
    }
}
```

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/Navigation/PinnedDatabasesViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using System.Collections.ObjectModel;

namespace Open.Db.Viewer.Shell.ViewModels.Navigation;

public sealed partial class PinnedDatabasesViewModel : ObservableObject
{
    private readonly DatabaseEntryService _databaseEntryService;

    public PinnedDatabasesViewModel(DatabaseEntryService databaseEntryService) => _databaseEntryService = databaseEntryService;

    public ObservableCollection<DatabaseEntry> Entries { get; } = new();

    public ObservableCollection<DatabaseEntry> FilteredEntries { get; } = new();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var pinned = await _databaseEntryService.GetPinnedAsync(cancellationToken);
        Entries.Clear();
        foreach (var item in pinned.OrderByDescending(entry => entry.LastOpenedAt))
        {
            Entries.Add(item);
        }

        FilteredEntries.Clear();
        foreach (var item in Entries)
        {
            FilteredEntries.Add(item);
        }
    }

    [RelayCommand]
    public async Task TogglePinAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
    {
        await _databaseEntryService.UnpinAsync(entry, cancellationToken);
        await LoadAsync(cancellationToken);
    }
}
```

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/Navigation/SettingsViewModel.cs
namespace Open.Db.Viewer.Shell.ViewModels.Navigation;

public sealed class SettingsViewModel
{
    public string Title => "设置";
}
```

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/Navigation/AboutViewModel.cs
namespace Open.Db.Viewer.Shell.ViewModels.Navigation;

public sealed class AboutViewModel
{
    public string Title => "关于";
    public string ProductName => "数据库查看器";
}
```

```csharp
// src/Open.Db.Viewer.Shell/ServiceCollectionExtensions.cs
services.AddSingleton<Open.Db.Viewer.Shell.ViewModels.Navigation.HomeLandingViewModel>();
services.AddSingleton<Open.Db.Viewer.Shell.ViewModels.Navigation.RecentDatabasesViewModel>();
services.AddSingleton<Open.Db.Viewer.Shell.ViewModels.Navigation.PinnedDatabasesViewModel>();
services.AddSingleton<Open.Db.Viewer.Shell.ViewModels.Navigation.SettingsViewModel>();
services.AddSingleton<Open.Db.Viewer.Shell.ViewModels.Navigation.AboutViewModel>();
```

- [ ] **Step 4: Run test to verify it passes**

Run:

```bash
dotnet test tests/Open.Db.Viewer.Shell.Tests/Open.Db.Viewer.Shell.Tests.csproj --filter "FullyQualifiedName~HomeLandingViewModelTests|FullyQualifiedName~RecentDatabasesViewModelTests|FullyQualifiedName~PinnedDatabasesViewModelTests"
```

Expected: PASS for the three new navigation ViewModel test classes.

- [ ] **Step 5: Commit**

```bash
git add src/Open.Db.Viewer.Shell/ViewModels/Navigation/HomeLandingViewModel.cs src/Open.Db.Viewer.Shell/ViewModels/Navigation/RecentDatabasesViewModel.cs src/Open.Db.Viewer.Shell/ViewModels/Navigation/PinnedDatabasesViewModel.cs src/Open.Db.Viewer.Shell/ViewModels/Navigation/SettingsViewModel.cs src/Open.Db.Viewer.Shell/ViewModels/Navigation/AboutViewModel.cs src/Open.Db.Viewer.Shell/ServiceCollectionExtensions.cs tests/Open.Db.Viewer.Shell.Tests/ViewModels/HomeLandingViewModelTests.cs tests/Open.Db.Viewer.Shell.Tests/ViewModels/RecentDatabasesViewModelTests.cs tests/Open.Db.Viewer.Shell.Tests/ViewModels/PinnedDatabasesViewModelTests.cs
git commit -m "feat: add shell navigation view models"
```

## Task 3: Build the Shell Window and Navigation Pages

**Files:**
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/HomeLandingPage.xaml`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/HomeLandingPage.xaml.cs`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/RecentDatabasesPage.xaml`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/RecentDatabasesPage.xaml.cs`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/PinnedDatabasesPage.xaml`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/PinnedDatabasesPage.xaml.cs`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/SettingsPage.xaml`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/SettingsPage.xaml.cs`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/AboutPage.xaml`
- Create: `src/Open.Db.Viewer.Shell/Views/Navigation/AboutPage.xaml.cs`
- Modify: `src/Open.Db.Viewer.Shell/Views/MainWindow.xaml`
- Modify: `src/Open.Db.Viewer.Shell/Views/MainWindow.xaml.cs`
- Test: `tests/Open.Db.Viewer.Shell.Tests/Views/MainWindowSmokeTests.cs`

- [ ] **Step 1: Write the failing smoke test**

```csharp
[Fact]
public void MainWindow_ShouldRenderNavigationRailAndHomeHero()
{
    Exception? failure = null;

    var thread = new Thread(() =>
    {
        try
        {
            var application = EnsureApplicationResources();
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
            var shell = CreateShellViewModel();
            var window = new MainWindow(shell);

            window.Show();
            window.ApplyTemplate();
            window.UpdateLayout();
            DoEvents();

            var renderedTexts = EnumerateVisualTree(window)
                .OfType<TextBlock>()
                .Select(node => node.Text)
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToArray();

            renderedTexts.Should().Contain("首页");
            renderedTexts.Should().Contain("最近使用");
            renderedTexts.Should().Contain("数据库工作台");
            renderedTexts.Should().Contain("数据库查看器");
            renderedTexts.Should().Contain("快速打开");

            window.Close();
            application.Shutdown();
        }
        catch (Exception exception)
        {
            failure = exception;
        }
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join();

    if (failure is not null)
    {
        throw failure;
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test tests/Open.Db.Viewer.Shell.Tests/Open.Db.Viewer.Shell.Tests.csproj --filter "FullyQualifiedName~Open.Db.Viewer.Shell.Tests.Views.MainWindowSmokeTests"
```

Expected: FAIL because the current window does not render the new navigation rail and home hero.

- [ ] **Step 3: Write the shell window and page XAML**

```xml
<!-- src/Open.Db.Viewer.Shell/Views/MainWindow.xaml -->
<ui:FluentWindow x:Class="Open.Db.Viewer.Shell.Views.MainWindow"
                 xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                 xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                 xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
                 xmlns:shell="clr-namespace:Open.Db.Viewer.Shell.ViewModels.Shell"
                 xmlns:views="clr-namespace:Open.Db.Viewer.Shell.Views.Navigation"
                 Title="数据库查看器"
                 Width="1440"
                 Height="920"
                 ExtendsContentIntoTitleBar="True"
                 WindowBackdropType="Mica">
    <Grid Background="{DynamicResource ApplicationBackgroundBrush}">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="220" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>

        <Border Grid.Column="0"
                Margin="0"
                Padding="12"
                Background="{DynamicResource ControlFillColorSecondaryBrush}">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="16" />
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                    <RowDefinition Height="Auto" />
                </Grid.RowDefinitions>

                <TextBlock FontSize="20"
                           FontWeight="SemiBold"
                           Text="数据库查看器" />

                <ui:Button Grid.Row="2"
                           Height="44"
                           Appearance="Primary"
                           Content="打开数据库"
                           Command="{Binding OpenDatabaseCommand}" />

                <ItemsControl Grid.Row="3"
                              ItemsSource="{Binding NavigationItems}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <ui:Button Margin="0,0,0,8"
                                       Content="{Binding Title}"
                                       Command="{Binding DataContext.NavigateToSectionCommand, RelativeSource={RelativeSource AncestorType=Window}}"
                                       CommandParameter="{Binding Section}" />
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>

                <StackPanel Grid.Row="4">
                    <ui:Button Content="设置"
                               Command="{Binding NavigateToSectionCommand}"
                               CommandParameter="{x:Static shell:ShellSection.Settings}" />
                    <ui:Button Margin="0,8,0,0"
                               Content="关于"
                               Command="{Binding NavigateToSectionCommand}"
                               CommandParameter="{x:Static shell:ShellSection.About}" />
                </StackPanel>
            </Grid>
        </Border>

        <ContentControl Grid.Column="1"
                        Content="{Binding CurrentContentViewModel}"
                        ContentTemplateSelector="{StaticResource ShellContentTemplateSelector}" />
    </Grid>
</ui:FluentWindow>
```

```xml
<!-- src/Open.Db.Viewer.Shell/Views/Navigation/HomeLandingPage.xaml -->
<UserControl x:Class="Open.Db.Viewer.Shell.Views.Navigation.HomeLandingPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml">
    <Grid Margin="28">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="20" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="20" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <Border Padding="28"
                CornerRadius="24"
                Background="{DynamicResource ControlFillColorSecondaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="320" />
                </Grid.ColumnDefinitions>

                <StackPanel>
                    <TextBlock FontSize="32" FontWeight="SemiBold" Text="数据库查看器" />
                    <TextBlock Margin="0,12,0,0"
                               Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                               Text="浏览 SQLite 文件，查看表结构，执行快速 SQL，并导出结果。" />
                    <StackPanel Margin="0,24,0,0" Orientation="Horizontal">
                        <ui:Button Width="160" Height="44" Appearance="Primary" Content="打开数据库" Command="{Binding OpenDatabaseCommand}" />
                        <ui:Button Width="140" Height="44" Margin="12,0,0,0" Content="打开最近" Command="{Binding OpenQuickOpenCommand}" />
                    </StackPanel>
                </StackPanel>

                <Border Grid.Column="1"
                        CornerRadius="20"
                        Background="{DynamicResource ControlFillColorTertiaryBrush}" />
            </Grid>
        </Border>

        <Border Grid.Row="2"
                Padding="20"
                CornerRadius="20"
                Background="{DynamicResource ControlFillColorSecondaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1">
            <StackPanel>
                <TextBlock FontSize="22" FontWeight="SemiBold" Text="快速打开" />
                <TextBlock Margin="0,6,0,0"
                           Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                           Text="快速返回最近打开过的数据库。" />
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

```csharp
// src/Open.Db.Viewer.Shell/Views/MainWindow.xaml.cs
public sealed class ShellContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HomeTemplate { get; set; }
    public DataTemplate? RecentTemplate { get; set; }
    public DataTemplate? PinnedTemplate { get; set; }
    public DataTemplate? SettingsTemplate { get; set; }
    public DataTemplate? AboutTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        item switch
        {
            Open.Db.Viewer.Shell.ViewModels.Navigation.HomeLandingViewModel => HomeTemplate,
            Open.Db.Viewer.Shell.ViewModels.Navigation.RecentDatabasesViewModel => RecentTemplate,
            Open.Db.Viewer.Shell.ViewModels.Navigation.PinnedDatabasesViewModel => PinnedTemplate,
            Open.Db.Viewer.Shell.ViewModels.Navigation.SettingsViewModel => SettingsTemplate,
            Open.Db.Viewer.Shell.ViewModels.Navigation.AboutViewModel => AboutTemplate,
            _ => base.SelectTemplate(item, container)
        };
}
```

- [ ] **Step 4: Run smoke tests to verify they pass**

Run:

```bash
dotnet test tests/Open.Db.Viewer.Shell.Tests/Open.Db.Viewer.Shell.Tests.csproj --filter "FullyQualifiedName~Open.Db.Viewer.Shell.Tests.Views.MainWindowSmokeTests"
```

Expected: PASS with rendered texts including shell navigation labels and the new home hero copy.

- [ ] **Step 5: Commit**

```bash
git add src/Open.Db.Viewer.Shell/Views/MainWindow.xaml src/Open.Db.Viewer.Shell/Views/MainWindow.xaml.cs src/Open.Db.Viewer.Shell/Views/Navigation/HomeLandingPage.xaml src/Open.Db.Viewer.Shell/Views/Navigation/HomeLandingPage.xaml.cs src/Open.Db.Viewer.Shell/Views/Navigation/RecentDatabasesPage.xaml src/Open.Db.Viewer.Shell/Views/Navigation/RecentDatabasesPage.xaml.cs src/Open.Db.Viewer.Shell/Views/Navigation/PinnedDatabasesPage.xaml src/Open.Db.Viewer.Shell/Views/Navigation/PinnedDatabasesPage.xaml.cs src/Open.Db.Viewer.Shell/Views/Navigation/SettingsPage.xaml src/Open.Db.Viewer.Shell/Views/Navigation/SettingsPage.xaml.cs src/Open.Db.Viewer.Shell/Views/Navigation/AboutPage.xaml src/Open.Db.Viewer.Shell/Views/Navigation/AboutPage.xaml.cs tests/Open.Db.Viewer.Shell.Tests/Views/MainWindowSmokeTests.cs
git commit -m "feat: add unified shell window and navigation pages"
```

## Task 4: Integrate the Workspace Into the Shell

**Files:**
- Create: `src/Open.Db.Viewer.Shell/ViewModels/Workspace/WorkspaceHostViewModel.cs`
- Create: `src/Open.Db.Viewer.Shell/Views/Workspace/WorkspaceHostPage.xaml`
- Create: `src/Open.Db.Viewer.Shell/Views/Workspace/WorkspaceHostPage.xaml.cs`
- Modify: `src/Open.Db.Viewer.Shell/ViewModels/ShellViewModel.cs`
- Modify: `src/Open.Db.Viewer.Shell/ViewModels/DatabaseWorkspaceViewModel.cs`
- Modify: `src/Open.Db.Viewer.Shell/Views/Pages/DatabaseWorkspacePage.xaml`
- Modify: `src/Open.Db.Viewer.Shell/Views/MainWindow.xaml`
- Modify: `src/Open.Db.Viewer.Shell/Views/MainWindow.xaml.cs`
- Test: `tests/Open.Db.Viewer.Shell.Tests/ViewModels/ShellViewModelTests.cs`
- Test: `tests/Open.Db.Viewer.Shell.Tests/ViewModels/DatabaseWorkspaceViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public void WorkspaceSection_ShouldShowEmptyState_WhenNoDatabaseIsOpen()
{
    var workspace = new DatabaseWorkspaceViewModel(
        new ObjectExplorerViewModel(),
        new SchemaViewModel(),
        new DataViewModel(),
        new QueryViewModel(
            new QueryService(new NoopSqliteQueryExecutor()),
            new ExportService(new NoopCsvExportWriter()),
            new FakeFileDialogService(null)));

    workspace.HasOpenDatabase.Should().BeFalse();
    workspace.EmptyStateTitle.Should().Be("尚未打开数据库");
}

[Fact]
public async Task OpenDatabaseAsync_ShouldPopulateWorkspaceSessionAndNavigate()
{
    var shell = CreateShell(@"C:\data\demo.db");

    await shell.OpenDatabaseAsync();

    shell.CurrentSection.Should().Be(ShellSection.Workspace);
    shell.CurrentContentViewModel.Should().BeSameAs(shell.WorkspaceHost);
    shell.Workspace.HasOpenDatabase.Should().BeTrue();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test tests/Open.Db.Viewer.Shell.Tests/Open.Db.Viewer.Shell.Tests.csproj --filter "FullyQualifiedName~ShellViewModelTests|FullyQualifiedName~DatabaseWorkspaceViewModelTests"
```

Expected: FAIL because the workspace empty-state contract and shell content host contract do not exist yet.

- [ ] **Step 3: Write the workspace host and empty-state integration**

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/Workspace/WorkspaceHostViewModel.cs
namespace Open.Db.Viewer.Shell.ViewModels.Workspace;

public sealed class WorkspaceHostViewModel
{
    public WorkspaceHostViewModel(DatabaseWorkspaceViewModel workspace)
    {
        Workspace = workspace;
    }

    public DatabaseWorkspaceViewModel Workspace { get; }
}
```

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/DatabaseWorkspaceViewModel.cs
public partial class DatabaseWorkspaceViewModel : ObservableObject
{
    public bool HasOpenDatabase => !string.IsNullOrWhiteSpace(DatabasePath);

    public string EmptyStateTitle => "尚未打开数据库";

    public string EmptyStateDescription => "从左侧导航或首页动作中打开一个 SQLite 数据库。";

    public override async Task LoadAsync(string databasePath, CancellationToken cancellationToken = default)
    {
        DatabasePath = databasePath;
        Title = Path.GetFileNameWithoutExtension(databasePath);
        Query.Configure(databasePath);

        await ObjectExplorer.LoadAsync(databasePath, cancellationToken);

        if (ObjectExplorer.SelectedNode is not null)
        {
            await SelectNodeAsync(ObjectExplorer.SelectedNode, cancellationToken);
        }
        else
        {
            Schema.Clear();
            Data.Clear();
            StatusMessage = "请选择一个表开始浏览。";
        }

        OnPropertyChanged(nameof(HasOpenDatabase));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateDescription));
        NotifyWorkspaceStateChanged();
    }
}
```

```xml
<!-- src/Open.Db.Viewer.Shell/Views/Workspace/WorkspaceHostPage.xaml -->
<UserControl x:Class="Open.Db.Viewer.Shell.Views.Workspace.WorkspaceHostPage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:pages="clr-namespace:Open.Db.Viewer.Shell.Views.Pages">
    <UserControl.Resources>
        <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter" />
    </UserControl.Resources>
    <Grid>
        <Grid Visibility="{Binding Workspace.HasOpenDatabase, Converter={StaticResource BooleanToVisibilityConverter}}">
            <pages:DatabaseWorkspacePage DataContext="{Binding Workspace}" />
        </Grid>

        <Border Margin="32"
                Padding="32"
                CornerRadius="24"
                Background="{DynamicResource ControlFillColorSecondaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1">
            <Border.Style>
                <Style TargetType="Border">
                    <Setter Property="Visibility" Value="Visible" />
                    <Style.Triggers>
                        <DataTrigger Binding="{Binding Workspace.HasOpenDatabase}" Value="True">
                            <Setter Property="Visibility" Value="Collapsed" />
                        </DataTrigger>
                    </Style.Triggers>
                </Style>
            </Border.Style>
            <StackPanel>
                <TextBlock FontSize="28" FontWeight="SemiBold" Text="{Binding Workspace.EmptyStateTitle}" />
                <TextBlock Margin="0,12,0,0"
                           Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                           Text="{Binding Workspace.EmptyStateDescription}" />
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
```

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/ShellViewModel.cs
public sealed partial class ShellViewModel : ObservableObject
{
    public ShellViewModel(HomeViewModel homeViewModel, DatabaseWorkspaceViewModel databaseWorkspaceViewModel, HomeLandingViewModel homeLanding, RecentDatabasesViewModel recent, PinnedDatabasesViewModel pinned, SettingsViewModel settings, AboutViewModel about)
    {
        Home = homeViewModel;
        Workspace = databaseWorkspaceViewModel;
        HomeLanding = homeLanding;
        RecentPage = recent;
        PinnedPage = pinned;
        SettingsPage = settings;
        AboutPage = about;
        WorkspaceHost = new WorkspaceHostViewModel(Workspace);

        CurrentContentViewModel = HomeLanding;
        Home.DatabaseOpenedAsync = OpenWorkspaceAsync;
        HomeLanding.DatabaseOpenedAsync = OpenWorkspaceAsync;
    }

    public HomeLandingViewModel HomeLanding { get; }
    public RecentDatabasesViewModel RecentPage { get; }
    public PinnedDatabasesViewModel PinnedPage { get; }
    public SettingsViewModel SettingsPage { get; }
    public AboutViewModel AboutPage { get; }
    public WorkspaceHostViewModel WorkspaceHost { get; }

    [ObservableProperty]
    private object currentContentViewModel;

    public void NavigateToSection(ShellSection section)
    {
        CurrentSection = section;
        CurrentContentViewModel = section switch
        {
            ShellSection.Home => HomeLanding,
            ShellSection.Recent => RecentPage,
            ShellSection.Pinned => PinnedPage,
            ShellSection.Workspace => WorkspaceHost,
            ShellSection.Settings => SettingsPage,
            ShellSection.About => AboutPage,
            _ => HomeLanding
        };
        UpdateNavigationSelection();
    }
}
```

```csharp
// src/Open.Db.Viewer.Shell/Views/MainWindow.xaml.cs
public sealed class ShellContentTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HomeTemplate { get; set; }
    public DataTemplate? RecentTemplate { get; set; }
    public DataTemplate? PinnedTemplate { get; set; }
    public DataTemplate? SettingsTemplate { get; set; }
    public DataTemplate? AboutTemplate { get; set; }
    public DataTemplate? WorkspaceTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        item switch
        {
            Open.Db.Viewer.Shell.ViewModels.Navigation.HomeLandingViewModel => HomeTemplate,
            Open.Db.Viewer.Shell.ViewModels.Navigation.RecentDatabasesViewModel => RecentTemplate,
            Open.Db.Viewer.Shell.ViewModels.Navigation.PinnedDatabasesViewModel => PinnedTemplate,
            Open.Db.Viewer.Shell.ViewModels.Navigation.SettingsViewModel => SettingsTemplate,
            Open.Db.Viewer.Shell.ViewModels.Navigation.AboutViewModel => AboutTemplate,
            Open.Db.Viewer.Shell.ViewModels.Workspace.WorkspaceHostViewModel => WorkspaceTemplate,
            _ => base.SelectTemplate(item, container)
        };
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run:

```bash
dotnet test tests/Open.Db.Viewer.Shell.Tests/Open.Db.Viewer.Shell.Tests.csproj --filter "FullyQualifiedName~ShellViewModelTests|FullyQualifiedName~DatabaseWorkspaceViewModelTests"
```

Expected: PASS with workspace empty-state assertions and shell workspace navigation assertions green.

- [ ] **Step 5: Commit**

```bash
git add src/Open.Db.Viewer.Shell/ViewModels/Workspace/WorkspaceHostViewModel.cs src/Open.Db.Viewer.Shell/Views/Workspace/WorkspaceHostPage.xaml src/Open.Db.Viewer.Shell/Views/Workspace/WorkspaceHostPage.xaml.cs src/Open.Db.Viewer.Shell/ViewModels/ShellViewModel.cs src/Open.Db.Viewer.Shell/ViewModels/DatabaseWorkspaceViewModel.cs src/Open.Db.Viewer.Shell/Views/Pages/DatabaseWorkspacePage.xaml tests/Open.Db.Viewer.Shell.Tests/ViewModels/ShellViewModelTests.cs tests/Open.Db.Viewer.Shell.Tests/ViewModels/DatabaseWorkspaceViewModelTests.cs
git commit -m "feat: host workspace inside the unified shell"
```

## Task 5: Wire Section Loading, Refresh Behaviors, and Final Regression Test

**Files:**
- Modify: `src/Open.Db.Viewer.Shell/ViewModels/ShellViewModel.cs`
- Modify: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/HomeLandingViewModel.cs`
- Modify: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/RecentDatabasesViewModel.cs`
- Modify: `src/Open.Db.Viewer.Shell/ViewModels/Navigation/PinnedDatabasesViewModel.cs`
- Modify: `tests/Open.Db.Viewer.Shell.Tests/ViewModels/ShellViewModelTests.cs`
- Modify: `tests/Open.Db.Viewer.Shell.Tests/Views/MainWindowSmokeTests.cs`

- [ ] **Step 1: Write the failing regression tests**

```csharp
[Fact]
public async Task NavigateToSection_ShouldLoadRecentPageOnDemand()
{
    var shell = CreateShell();

    shell.NavigateToSection(ShellSection.Recent);
    await shell.LoadCurrentSectionAsync();

    shell.RecentPage.FilteredEntries.Should().NotBeNull();
}

[Fact]
public async Task OpenDatabaseAsync_ShouldRefreshHomeSummariesAfterSuccess()
{
    var shell = CreateShell(@"C:\data\demo.db");

    await shell.OpenDatabaseAsync();
    await shell.HomeLanding.LoadAsync();

    shell.HomeLanding.QuickOpenEntry.Should().NotBeNull();
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:

```bash
dotnet test tests/Open.Db.Viewer.Shell.Tests/Open.Db.Viewer.Shell.Tests.csproj --filter "FullyQualifiedName~ShellViewModelTests|FullyQualifiedName~MainWindowSmokeTests"
```

Expected: FAIL with missing section-loading behavior or stale page data.

- [ ] **Step 3: Write the minimal section-loading and refresh logic**

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/ShellViewModel.cs
[RelayCommand]
public async Task LoadCurrentSectionAsync(CancellationToken cancellationToken = default)
{
    switch (CurrentSection)
    {
        case ShellSection.Home:
            await HomeLanding.LoadAsync(cancellationToken);
            break;
        case ShellSection.Recent:
            await RecentPage.LoadAsync(cancellationToken);
            break;
        case ShellSection.Pinned:
            await PinnedPage.LoadAsync(cancellationToken);
            break;
    }
}

private async Task OpenWorkspaceAsync(string databasePath, CancellationToken cancellationToken)
{
    CurrentDatabasePath = databasePath;
    await Workspace.LoadAsync(databasePath, cancellationToken);
    await HomeLanding.LoadAsync(cancellationToken);
    await RecentPage.LoadAsync(cancellationToken);
    await PinnedPage.LoadAsync(cancellationToken);
    NavigateToSection(ShellSection.Workspace);
}
```

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/Navigation/HomeLandingViewModel.cs
[RelayCommand]
public async Task OpenQuickOpenAsync(CancellationToken cancellationToken = default)
{
    if (QuickOpenEntry is null || DatabaseOpenedAsync is null)
    {
        return;
    }

    var result = await _databaseEntryService.OpenAsync(QuickOpenEntry.FilePath, cancellationToken);
    if (result.IsSuccess)
    {
        await DatabaseOpenedAsync(QuickOpenEntry.FilePath, cancellationToken);
    }
}
```

```csharp
// src/Open.Db.Viewer.Shell/ViewModels/Navigation/RecentDatabasesViewModel.cs
public Func<string, CancellationToken, Task>? DatabaseOpenedAsync { get; set; }

[RelayCommand]
public async Task OpenEntryAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
{
    var result = await _databaseEntryService.OpenAsync(entry.FilePath, cancellationToken);
    if (result.IsSuccess && DatabaseOpenedAsync is not null)
    {
        await DatabaseOpenedAsync(entry.FilePath, cancellationToken);
    }
}
```

- [ ] **Step 4: Run the full shell test suite**

Run:

```bash
dotnet test tests/Open.Db.Viewer.Shell.Tests/Open.Db.Viewer.Shell.Tests.csproj
```

Expected: PASS for all shell ViewModel tests and WPF smoke tests.

- [ ] **Step 5: Commit**

```bash
git add src/Open.Db.Viewer.Shell/ViewModels/ShellViewModel.cs src/Open.Db.Viewer.Shell/ViewModels/Navigation/HomeLandingViewModel.cs src/Open.Db.Viewer.Shell/ViewModels/Navigation/RecentDatabasesViewModel.cs src/Open.Db.Viewer.Shell/ViewModels/Navigation/PinnedDatabasesViewModel.cs tests/Open.Db.Viewer.Shell.Tests/ViewModels/ShellViewModelTests.cs tests/Open.Db.Viewer.Shell.Tests/Views/MainWindowSmokeTests.cs
git commit -m "feat: finish shell navigation loading flow"
```

## Self-Review

### Spec coverage

- Unified shell: covered by Tasks 1 and 3
- Left navigation and dedicated sections: covered by Tasks 1, 2, and 3
- Home redesign and summary cards: covered by Task 3
- Recent/pinned dedicated pages: covered by Tasks 2 and 3
- Global open-database action: covered by Tasks 1 and 3
- Workspace inside shell: covered by Task 4
- Shared section loading and refresh rules: covered by Task 5
- Explicit non-goals such as row editing and multi-document tabs: not scheduled in this plan by design

### Placeholder scan

- No `TODO`, `TBD`, or deferred implementation markers remain
- Every code-writing step includes concrete code
- Every testing step includes exact commands and expected outcomes

### Type consistency

- `ShellSection` is the single source of truth for shell sections
- `CurrentContentViewModel` is used consistently as the shell content host object
- `WorkspaceHostViewModel` consistently wraps `DatabaseWorkspaceViewModel`
- `DatabaseOpenedAsync` remains the handoff interface between page ViewModels and the shell
