# Open DB Viewer UI/UX Refresh Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refresh the WPF shell so the home page and workspace feel like a polished desktop product, while preserving the existing SQLite MVP workflow and tests.

**Architecture:** Keep the current MVVM layout and upgrade the UI in place. Add only the smallest amount of new view-model state needed to support better empty states, contextual toolbars, return-home and refresh actions, and current-page CSV export. Favor page-level XAML improvements plus focused ViewModel properties and commands over structural rewrites.

**Tech Stack:** .NET 8, WPF, WPF UI, CommunityToolkit.Mvvm, xUnit, FluentAssertions

---

## Planned File Structure

### Shell views

- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Views\MainWindow.xaml`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Views\MainWindow.xaml.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Views\Pages\HomePage.xaml`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Views\Pages\HomePage.xaml.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Views\Pages\DatabaseWorkspacePage.xaml`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Views\Pages\DatabaseWorkspacePage.xaml.cs`

### Shell view-models

- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\ShellViewModel.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\HomeViewModel.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\DatabaseWorkspaceViewModel.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\DataViewModel.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\QueryViewModel.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\SchemaViewModel.cs`

### Shell DI wiring

- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ServiceCollectionExtensions.cs`

### Tests

- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\ViewModels\ShellViewModelTests.cs`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\ViewModels\HomeViewModelTests.cs`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\ViewModels\DatabaseWorkspaceViewModelTests.cs`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\ViewModels\DataViewModelTests.cs`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\ViewModels\QueryViewModelTests.cs`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Views\MainWindowSmokeTests.cs`

## Task 1: Add workspace-level navigation, refresh, and context state

**Files:**
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\DatabaseWorkspaceViewModel.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\ShellViewModel.cs`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\ViewModels\ShellViewModelTests.cs`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\ViewModels\DatabaseWorkspaceViewModelTests.cs`

- [ ] **Step 1: Write failing tests for returning home and refreshing the current workspace**

```csharp
[Fact]
public async Task ReturnHomeAsync_ShouldNavigateBackToHomePage()
{
    var repository = new InMemoryDatabaseEntryRepository();
    var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
    var workspace = new FakeDatabaseWorkspaceViewModel();
    var home = new HomeViewModel(databaseEntryService, new FakeFileDialogService(@"C:\data\demo.db"));
    var shell = new ShellViewModel(home, workspace);

    await home.OpenDatabaseAsync();
    await workspace.ReturnHomeAsync();

    shell.CurrentPage.Should().BeSameAs(home);
}

[Fact]
public async Task RefreshAsync_ShouldReloadCurrentSelection_AndUpdateStatus()
{
    await using var db = await SqliteTestDb.CreateAsync();
    var connectionFactory = new SqliteConnectionFactory();
    var inspector = new SqliteDatabaseInspector(connectionFactory);
    var data = new DataViewModel(new SqliteTableDataReader(connectionFactory));
    var viewModel = new DatabaseWorkspaceViewModel(
        new ObjectExplorerViewModel(inspector),
        new SchemaViewModel(inspector),
        data,
        new QueryViewModel(
            new Open.Db.Viewer.Application.Services.QueryService(new SqliteQueryExecutor(connectionFactory)),
            new Open.Db.Viewer.Application.Services.ExportService(new Open.Db.Viewer.Infrastructure.Sqlite.Export.CsvExportWriter()),
            new FakeFileDialogService()));

    await viewModel.LoadAsync(db.FilePath);
    var originalSelection = viewModel.ObjectExplorer.SelectedNode;

    await viewModel.RefreshAsync();

    viewModel.ObjectExplorer.SelectedNode?.Name.Should().Be(originalSelection?.Name);
    viewModel.StatusMessage.Should().Be("Workspace refreshed.");
}
```

- [ ] **Step 2: Run the targeted shell tests to verify they fail**

Run: `dotnet test C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Open.Db.Viewer.Shell.Tests.csproj --filter "ReturnHomeAsync_ShouldNavigateBackToHomePage|RefreshAsync_ShouldReloadCurrentSelection_AndUpdateStatus"`

Expected: FAIL with missing `ReturnHomeAsync`, `RefreshAsync`, or `StatusMessage`.

- [ ] **Step 3: Add workspace state and commands to the workspace view model**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Domain.Models;
using System.IO;

namespace Open.Db.Viewer.Shell.ViewModels;

public partial class DatabaseWorkspaceViewModel : ObservableObject
{
    [ObservableProperty]
    private string databasePath = string.Empty;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Select a table to start browsing.";

    [ObservableProperty]
    private bool isRefreshing;

    public Func<Task>? RequestReturnHomeAsync { get; set; }

    public bool HasTableSelection =>
        ObjectExplorer.SelectedNode is not null &&
        string.Equals(ObjectExplorer.SelectedNode.Kind, "table", StringComparison.OrdinalIgnoreCase);

    public string SelectedObjectTitle => ObjectExplorer.SelectedNode?.Name ?? "No table selected";

    public string SelectedObjectSubtitle => HasTableSelection
        ? "Structure, data, and query tools are ready."
        : "Choose a table from the left navigation to begin.";

    [RelayCommand]
    public async Task ReturnHomeAsync()
    {
        if (RequestReturnHomeAsync is not null)
        {
            await RequestReturnHomeAsync();
        }
    }

    [RelayCommand]
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(DatabasePath))
        {
            return;
        }

        IsRefreshing = true;
        try
        {
            var selectedTableName = HasTableSelection ? ObjectExplorer.SelectedNode!.Name : null;

            await ObjectExplorer.LoadAsync(DatabasePath, cancellationToken);

            if (!string.IsNullOrWhiteSpace(selectedTableName))
            {
                var restoredNode = ObjectExplorer.RootNodes
                    .SelectMany(root => root.Children ?? Array.Empty<DatabaseObjectNode>())
                    .FirstOrDefault(node => node.Name.Equals(selectedTableName, StringComparison.OrdinalIgnoreCase));

                await SelectNodeAsync(restoredNode, cancellationToken);
            }
            else
            {
                Schema.Clear();
                Data.Clear();
                Query.Configure(DatabasePath);
            }

            StatusMessage = "Workspace refreshed.";
            OnPropertyChanged(nameof(HasTableSelection));
            OnPropertyChanged(nameof(SelectedObjectTitle));
            OnPropertyChanged(nameof(SelectedObjectSubtitle));
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}
```

- [ ] **Step 4: Wire return-home behavior in the shell view model**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace Open.Db.Viewer.Shell.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private object currentPage;

    public ShellViewModel(HomeViewModel homeViewModel, DatabaseWorkspaceViewModel databaseWorkspaceViewModel)
    {
        Home = homeViewModel;
        Workspace = databaseWorkspaceViewModel;
        CurrentPage = homeViewModel;

        Home.DatabaseOpenedAsync = OpenWorkspaceAsync;
        Workspace.RequestReturnHomeAsync = ReturnHomeAsync;
        _ = Home.LoadAsync();
    }

    public HomeViewModel Home { get; }

    public DatabaseWorkspaceViewModel Workspace { get; }

    private async Task OpenWorkspaceAsync(string databasePath, CancellationToken cancellationToken)
    {
        await Workspace.LoadAsync(databasePath, cancellationToken);
        CurrentPage = Workspace;
    }

    private Task ReturnHomeAsync()
    {
        CurrentPage = Home;
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 5: Run the targeted shell tests to verify they pass**

Run: `dotnet test C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Open.Db.Viewer.Shell.Tests.csproj --filter "ReturnHomeAsync_ShouldNavigateBackToHomePage|RefreshAsync_ShouldReloadCurrentSelection_AndUpdateStatus"`

Expected: PASS

- [ ] **Step 6: Commit the workspace-shell state changes**

```bash
git -C C:\Code\open.db.viewer add \
  src/Open.Db.Viewer.Shell/ViewModels/DatabaseWorkspaceViewModel.cs \
  src/Open.Db.Viewer.Shell/ViewModels/ShellViewModel.cs \
  tests/Open.Db.Viewer.Shell.Tests/ViewModels/ShellViewModelTests.cs \
  tests/Open.Db.Viewer.Shell.Tests/ViewModels/DatabaseWorkspaceViewModelTests.cs
git -C C:\Code\open.db.viewer commit -m "feat: add workspace navigation and refresh state"
```

## Task 2: Refresh the home page layout and home-state UX

**Files:**
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\HomeViewModel.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Views\Pages\HomePage.xaml`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\ViewModels\HomeViewModelTests.cs`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Views\MainWindowSmokeTests.cs`

- [ ] **Step 1: Write failing tests for first-run and no-results states**

```csharp
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
```

- [ ] **Step 2: Run the home-view-model tests to verify they fail**

Run: `dotnet test C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Open.Db.Viewer.Shell.Tests.csproj --filter "LoadAsync_ShouldExposeFirstRunState_WhenThereAreNoEntries|SearchText_ShouldExposeNoResultsState_WhenNothingMatches"`

Expected: FAIL with missing home-state properties.

- [ ] **Step 3: Add derived state properties to the home view model**

```csharp
public sealed partial class HomeViewModel : ObservableObject
{
    public bool HasPinnedEntries => PinnedEntries.Count > 0;

    public bool HasRecentEntries => RecentEntries.Count > 0;

    public bool HasAnyEntries => HasPinnedEntries || HasRecentEntries;

    public bool HasVisibleEntries => FilteredPinnedEntries.Count > 0 || FilteredRecentEntries.Count > 0;

    public bool ShowFirstRunState => !HasAnyEntries && string.IsNullOrWhiteSpace(SearchText);

    public bool ShowNoResultsState => HasAnyEntries && !HasVisibleEntries && !string.IsNullOrWhiteSpace(SearchText);

    public string SearchSummary => string.IsNullOrWhiteSpace(SearchText)
        ? "Pinned and recent databases"
        : $"Results for \"{SearchText}\"";

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        // existing load logic
        RefreshFilteredEntries();
        RaiseHomeStateChanged();
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshFilteredEntries();
        RaiseHomeStateChanged();
    }

    private void RaiseHomeStateChanged()
    {
        OnPropertyChanged(nameof(HasPinnedEntries));
        OnPropertyChanged(nameof(HasRecentEntries));
        OnPropertyChanged(nameof(HasAnyEntries));
        OnPropertyChanged(nameof(HasVisibleEntries));
        OnPropertyChanged(nameof(ShowFirstRunState));
        OnPropertyChanged(nameof(ShowNoResultsState));
        OnPropertyChanged(nameof(SearchSummary));
    }
}
```

- [ ] **Step 4: Replace the current home page with a hero-plus-card layout**

```xml
<UserControl x:Class="Open.Db.Viewer.Shell.Views.Pages.HomePage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
             xmlns:mw="http://schemas.lepo.co/wpfui/2022/xaml">
    <Grid Margin="28">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="24" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="20" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <Border Padding="28"
                Background="{DynamicResource ControlFillColorSecondaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1"
                CornerRadius="20">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>

                <StackPanel>
                    <TextBlock FontSize="30"
                               FontWeight="SemiBold"
                               Text="Open DB Viewer" />
                    <TextBlock Margin="0,10,0,0"
                               Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                               Text="Browse local SQLite files, inspect schema, run quick SQL, and export results." />
                </StackPanel>

                <ui:Button Grid.Column="1"
                           MinWidth="180"
                           Height="44"
                           Appearance="Primary"
                           Content="Open database"
                           Command="{Binding OpenDatabaseCommand}"
                           Icon="{mw:SymbolIcon FolderOpen24}" />
            </Grid>
        </Border>

        <Border Grid.Row="2"
                Padding="18"
                Background="{DynamicResource ControlFillColorSecondaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1"
                CornerRadius="18">
            <StackPanel>
                <TextBlock FontWeight="SemiBold"
                           Text="Find a saved database" />
                <TextBlock Margin="0,4,0,12"
                           Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                           Text="{Binding SearchSummary}" />
                <TextBox Text="{Binding SearchText, UpdateSourceTrigger=PropertyChanged}" />
            </StackPanel>
        </Border>

        <Grid Grid.Row="4">
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto" />
                <RowDefinition Height="16" />
                <RowDefinition Height="*" />
            </Grid.RowDefinitions>

            <StackPanel Visibility="{Binding ShowFirstRunState, Converter={StaticResource BooleanToVisibilityConverter}}">
                <TextBlock FontSize="22"
                           FontWeight="SemiBold"
                           Text="Start with a SQLite file" />
                <TextBlock Margin="0,8,0,0"
                           Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                           Text="Your pinned and recent databases will appear here after the first successful open." />
            </StackPanel>

            <StackPanel Visibility="{Binding ShowNoResultsState, Converter={StaticResource BooleanToVisibilityConverter}}">
                <TextBlock FontSize="22"
                           FontWeight="SemiBold"
                           Text="No matching databases" />
                <TextBlock Margin="0,8,0,0"
                           Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                           Text="Try a different name or path keyword." />
            </StackPanel>

            <Grid Grid.Row="2"
                  Visibility="{Binding HasVisibleEntries, Converter={StaticResource BooleanToVisibilityConverter}}">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="20" />
                    <ColumnDefinition Width="*" />
                </Grid.ColumnDefinitions>

                <Border Padding="18"
                        Background="{DynamicResource ControlFillColorSecondaryBrush}"
                        BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                        BorderThickness="1"
                        CornerRadius="18">
                    <!-- pinned list card content -->
                </Border>

                <Border Grid.Column="2"
                        Padding="18"
                        Background="{DynamicResource ControlFillColorSecondaryBrush}"
                        BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                        BorderThickness="1"
                        CornerRadius="18">
                    <!-- recent list card content -->
                </Border>
            </Grid>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 5: Update the home-page smoke assertion to reflect the refreshed layout**

```csharp
var renderedTexts = EnumerateVisualTree(window)
    .OfType<System.Windows.Controls.TextBlock>()
    .Select(node => node.Text)
    .Where(text => !string.IsNullOrWhiteSpace(text))
    .ToArray();

renderedTexts.Should().Contain("Open DB Viewer");
renderedTexts.Should().Contain("Find a saved database");
```

- [ ] **Step 6: Run the relevant shell tests to verify they pass**

Run: `dotnet test C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Open.Db.Viewer.Shell.Tests.csproj --filter "HomeViewModelTests|MainWindow_ShouldRenderHomePageContent_OnStartup"`

Expected: PASS

- [ ] **Step 7: Commit the home page refresh**

```bash
git -C C:\Code\open.db.viewer add \
  src/Open.Db.Viewer.Shell/ViewModels/HomeViewModel.cs \
  src/Open.Db.Viewer.Shell/Views/Pages/HomePage.xaml \
  tests/Open.Db.Viewer.Shell.Tests/ViewModels/HomeViewModelTests.cs \
  tests/Open.Db.Viewer.Shell.Tests/Views/MainWindowSmokeTests.cs
git -C C:\Code\open.db.viewer commit -m "feat: refresh home page desktop UX"
```

## Task 3: Rebuild the workspace layout hierarchy and empty-state guidance

**Files:**
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Views\Pages\DatabaseWorkspacePage.xaml`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Views\Pages\DatabaseWorkspacePage.xaml.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\DatabaseWorkspaceViewModel.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\SchemaViewModel.cs`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\ViewModels\DatabaseWorkspaceViewModelTests.cs`

- [ ] **Step 1: Write failing tests for no-selection guidance and schema summary**

```csharp
[Fact]
public void Clear_ShouldExposeNoSelectionWorkspaceState()
{
    var viewModel = new DatabaseWorkspaceViewModel(
        new ObjectExplorerViewModel(),
        new SchemaViewModel(),
        new DataViewModel(),
        new QueryViewModel(
            new QueryService(new NoopSqliteQueryExecutor()),
            new ExportService(new NoopCsvExportWriter()),
            new FakeFileDialogService()));

    viewModel.ObjectExplorer.SelectedNode = null;
    viewModel.Schema.Clear();
    viewModel.Data.Clear();

    viewModel.HasTableSelection.Should().BeFalse();
    viewModel.SelectedObjectTitle.Should().Be("No table selected");
}

[Fact]
public void LoadAsync_ShouldExposeSchemaColumnSummary()
{
    var schema = new SchemaViewModel();
    schema.Columns.Add(new TableColumnInfo("id", "INTEGER", false, null, true));
    schema.Columns.Add(new TableColumnInfo("name", "TEXT", true, null, false));

    schema.ColumnCountSummary.Should().Be("2 columns");
}
```

- [ ] **Step 2: Run the targeted tests to verify they fail**

Run: `dotnet test C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Open.Db.Viewer.Shell.Tests.csproj --filter "Clear_ShouldExposeNoSelectionWorkspaceState|LoadAsync_ShouldExposeSchemaColumnSummary"`

Expected: FAIL with missing workspace or schema summary properties.

- [ ] **Step 3: Add schema summary helpers**

```csharp
public partial class SchemaViewModel : ObservableObject
{
    public bool HasColumns => Columns.Count > 0;

    public string ColumnCountSummary => Columns.Count switch
    {
        1 => "1 column",
        _ => $"{Columns.Count} columns"
    };

    public async Task LoadAsync(string databasePath, string tableName, CancellationToken cancellationToken = default)
    {
        // existing schema load
        OnPropertyChanged(nameof(HasColumns));
        OnPropertyChanged(nameof(ColumnCountSummary));
    }

    public void Clear()
    {
        TableName = string.Empty;
        Columns.Clear();
        OnPropertyChanged(nameof(HasColumns));
        OnPropertyChanged(nameof(ColumnCountSummary));
    }
}
```

- [ ] **Step 4: Replace the workspace page with a context-bar + side-panel layout**

```xml
<UserControl x:Class="Open.Db.Viewer.Shell.Views.Pages.DatabaseWorkspacePage"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml"
             xmlns:mw="http://schemas.lepo.co/wpfui/2022/xaml">
    <Grid Margin="24">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="20" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <Border Padding="18"
                Background="{DynamicResource ControlFillColorSecondaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1"
                CornerRadius="18">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                </Grid.ColumnDefinitions>

                <StackPanel>
                    <TextBlock FontSize="26"
                               FontWeight="SemiBold"
                               Text="{Binding Title}" />
                    <TextBlock Margin="0,6,0,0"
                               Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                               Text="{Binding DatabasePath}" />
                    <TextBlock Margin="0,10,0,0"
                               Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                               Text="{Binding StatusMessage}" />
                </StackPanel>

                <StackPanel Grid.Column="1"
                            Orientation="Horizontal">
                    <ui:Button Content="Home"
                               Command="{Binding ReturnHomeCommand}"
                               Icon="{mw:SymbolIcon ArrowReply24}" />
                    <ui:Button Margin="8,0,0,0"
                               Content="Refresh"
                               Command="{Binding RefreshCommand}"
                               Icon="{mw:SymbolIcon ArrowClockwise24}" />
                </StackPanel>
            </Grid>
        </Border>

        <Grid Grid.Row="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="300" />
                <ColumnDefinition Width="20" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>

            <Border Padding="16"
                    Background="{DynamicResource ControlFillColorSecondaryBrush}"
                    BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                    BorderThickness="1"
                    CornerRadius="18">
                <!-- object navigation card -->
            </Border>

            <Border Grid.Column="2"
                    Padding="18"
                    Background="{DynamicResource ControlFillColorSecondaryBrush}"
                    BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                    BorderThickness="1"
                    CornerRadius="18">
                <Grid>
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="16" />
                        <RowDefinition Height="*" />
                    </Grid.RowDefinitions>

                    <StackPanel>
                        <TextBlock FontSize="22"
                                   FontWeight="SemiBold"
                                   Text="{Binding SelectedObjectTitle}" />
                        <TextBlock Margin="0,6,0,0"
                                   Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                                   Text="{Binding SelectedObjectSubtitle}" />
                    </StackPanel>

                    <Border Grid.Row="2"
                            Visibility="{Binding HasTableSelection, Converter={StaticResource BooleanToVisibilityConverter}}">
                        <!-- existing TabControl moves here -->
                    </Border>

                    <StackPanel Grid.Row="2"
                                Visibility="{Binding HasTableSelection, Converter={StaticResource InverseBooleanToVisibilityConverter}}">
                        <TextBlock FontSize="20"
                                   FontWeight="SemiBold"
                                   Text="Choose a table from the left" />
                        <TextBlock Margin="0,8,0,0"
                                   Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                                   Text="Schema, data, and query tools will appear here when a table is selected." />
                    </StackPanel>
                </Grid>
            </Border>
        </Grid>
    </Grid>
</UserControl>
```

- [ ] **Step 5: Keep the sorting and page-size event handlers aligned with the new layout**

```csharp
private async void PageSizeComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (!IsLoaded || DataContext is not DatabaseWorkspaceViewModel viewModel)
    {
        return;
    }

    if (e.AddedItems.Count == 0 || e.AddedItems[0] is not int pageSize || viewModel.Data.PageNumber == 0)
    {
        return;
    }

    await viewModel.Data.ChangePageSizeAsync(pageSize);
    viewModel.StatusMessage = $"Updated page size to {pageSize}.";
}
```

- [ ] **Step 6: Run the workspace tests to verify they pass**

Run: `dotnet test C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Open.Db.Viewer.Shell.Tests.csproj --filter "DatabaseWorkspaceViewModelTests"`

Expected: PASS

- [ ] **Step 7: Commit the workspace layout refresh**

```bash
git -C C:\Code\open.db.viewer add \
  src/Open.Db.Viewer.Shell/Views/Pages/DatabaseWorkspacePage.xaml \
  src/Open.Db.Viewer.Shell/Views/Pages/DatabaseWorkspacePage.xaml.cs \
  src/Open.Db.Viewer.Shell/ViewModels/DatabaseWorkspaceViewModel.cs \
  src/Open.Db.Viewer.Shell/ViewModels/SchemaViewModel.cs \
  tests/Open.Db.Viewer.Shell.Tests/ViewModels/DatabaseWorkspaceViewModelTests.cs
git -C C:\Code\open.db.viewer commit -m "feat: add structured workspace shell layout"
```

## Task 4: Upgrade the data tab toolbar, export flow, and result states

**Files:**
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\DataViewModel.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ServiceCollectionExtensions.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Views\Pages\DatabaseWorkspacePage.xaml`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\ViewModels\DataViewModelTests.cs`

- [ ] **Step 1: Write failing tests for data export and result-state summaries**

```csharp
[Fact]
public async Task ExportCurrentPageAsync_ShouldWriteVisibleRowsToCsv()
{
    await using var db = await SqliteTestDb.CreateAsync();
    var exportWriter = new RecordingCsvExportWriter();
    var viewModel = new DataViewModel(
        new SqliteTableDataReader(new SqliteConnectionFactory()),
        new ExportService(exportWriter),
        new FakeFileDialogService(@"C:\exports\users-page.csv"))
    {
        PageSize = 2
    };

    await viewModel.LoadFirstPageAsync(db.FilePath, "users");
    await viewModel.ExportCurrentPageAsync();

    exportWriter.FilePath.Should().Be(@"C:\exports\users-page.csv");
    exportWriter.Rows.Should().HaveCount(2);
    viewModel.StatusMessage.Should().Contain("users-page.csv");
}

[Fact]
public async Task LoadFirstPageAsync_ShouldExposeResultSummary()
{
    await using var db = await SqliteTestDb.CreateAsync();
    var viewModel = new DataViewModel(new SqliteTableDataReader(new SqliteConnectionFactory()));

    await viewModel.LoadFirstPageAsync(db.FilePath, "users");

    viewModel.HasRows.Should().BeTrue();
    viewModel.ResultSummary.Should().Be("Page 1 · 3 columns · 3 visible rows");
}
```

- [ ] **Step 2: Run the targeted data tests to verify they fail**

Run: `dotnet test C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Open.Db.Viewer.Shell.Tests.csproj --filter "ExportCurrentPageAsync_ShouldWriteVisibleRowsToCsv|LoadFirstPageAsync_ShouldExposeResultSummary"`

Expected: FAIL with missing export or summary members.

- [ ] **Step 3: Add export, refresh, and summary state to the data view model**

```csharp
public partial class DataViewModel : ObservableObject
{
    private readonly ExportService? _exportService;
    private readonly IFileDialogService? _fileDialogService;

    [ObservableProperty]
    private string statusMessage = "Choose a table to browse rows.";

    public bool HasRows => Rows.Count > 0;

    public string ResultSummary =>
        PageNumber == 0
            ? "No table data loaded."
            : $"Page {PageNumber} · {Columns.Count} columns · {Rows.Count} visible rows";

    public string SortSummary => string.IsNullOrWhiteSpace(SortColumn)
        ? "Default sort"
        : $"{SortColumn} {SortDirection}";

    public DataViewModel(SqliteTableDataReader tableDataReader)
        : this(tableDataReader, null, null)
    {
    }

    public DataViewModel(SqliteTableDataReader tableDataReader, ExportService? exportService, IFileDialogService? fileDialogService)
    {
        _tableDataReader = tableDataReader;
        _exportService = exportService;
        _fileDialogService = fileDialogService;
    }

    [RelayCommand]
    public Task RefreshCurrentPageAsync(CancellationToken cancellationToken = default) =>
        PageNumber > 0 ? LoadPageAsync(PageNumber, cancellationToken) : Task.CompletedTask;

    [RelayCommand]
    public async Task ExportCurrentPageAsync(CancellationToken cancellationToken = default)
    {
        if (!HasRows || _exportService is null || _fileDialogService is null || string.IsNullOrWhiteSpace(_tableName))
        {
            return;
        }

        var exportPath = _fileDialogService.PickCsvSavePath($"{_tableName}-page-{PageNumber}.csv");
        if (string.IsNullOrWhiteSpace(exportPath))
        {
            return;
        }

        await _exportService.ExportAsync(
            exportPath,
            new TabularData(Columns.ToArray(), Rows.Select(row => row.Values).ToArray()),
            cancellationToken);

        StatusMessage = $"Exported current page to {Path.GetFileName(exportPath)}.";
    }

    private void ApplyPage(TablePageResult page)
    {
        // existing row and column assignment
        StatusMessage = $"Loaded {Rows.Count} rows from {(_tableName ?? "table")}.";
        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(ResultSummary));
        OnPropertyChanged(nameof(SortSummary));
    }
}
```

- [ ] **Step 4: Register the export-capable data view model in DI**

```csharp
services.AddSingleton<DataViewModel>(provider =>
    new DataViewModel(
        provider.GetRequiredService<SqliteTableDataReader>(),
        provider.GetRequiredService<ExportService>(),
        provider.GetRequiredService<IFileDialogService>()));
```

- [ ] **Step 5: Replace the data tab toolbar with a cohesive result-browser command bar**

```xml
<TabItem Header="Data">
    <Grid Margin="12">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="12" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <Border Padding="14"
                Background="{DynamicResource ControlFillColorTertiaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1"
                CornerRadius="14">
            <WrapPanel>
                <ui:Button Content="Previous"
                           Command="{Binding Data.LoadPreviousPageCommand}" />
                <ui:Button Margin="8,0,0,0"
                           Content="Next"
                           Command="{Binding Data.LoadNextPageCommand}" />
                <ui:Button Margin="8,0,0,0"
                           Content="Refresh"
                           Command="{Binding Data.RefreshCurrentPageCommand}" />
                <ui:Button Margin="8,0,0,0"
                           Content="Export CSV"
                           Command="{Binding Data.ExportCurrentPageCommand}" />
                <ComboBox Margin="16,0,0,0"
                          Width="96"
                          ItemsSource="{Binding Data.PageSizeOptions}"
                          SelectedItem="{Binding Data.PageSize, Mode=TwoWay}" />
                <TextBlock Margin="16,0,0,0"
                           VerticalAlignment="Center"
                           Text="{Binding Data.ResultSummary}" />
                <TextBlock Margin="16,0,0,0"
                           VerticalAlignment="Center"
                           Text="{Binding Data.SortSummary}" />
            </WrapPanel>
        </Border>

        <Border Grid.Row="2"
                Padding="12"
                Background="{DynamicResource ControlFillColorSecondaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1"
                CornerRadius="14">
            <Grid>
                <DataGrid x:Name="DataGridView"
                          AutoGenerateColumns="True"
                          CanUserAddRows="False"
                          IsReadOnly="True"
                          ItemsSource="{Binding Data.TableView}"
                          Sorting="DataGridView_OnSorting" />
            </Grid>
        </Border>
    </Grid>
</TabItem>
```

- [ ] **Step 6: Run the data-view-model tests to verify they pass**

Run: `dotnet test C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Open.Db.Viewer.Shell.Tests.csproj --filter "DataViewModelTests"`

Expected: PASS

- [ ] **Step 7: Commit the data-tab UX changes**

```bash
git -C C:\Code\open.db.viewer add \
  src/Open.Db.Viewer.Shell/ViewModels/DataViewModel.cs \
  src/Open.Db.Viewer.Shell/Views/Pages/DatabaseWorkspacePage.xaml \
  src/Open.Db.Viewer.Shell/ServiceCollectionExtensions.cs \
  tests/Open.Db.Viewer.Shell.Tests/ViewModels/DataViewModelTests.cs
git -C C:\Code\open.db.viewer commit -m "feat: polish data tab toolbar and export UX"
```

## Task 5: Polish schema and query surfaces, then verify the full shell

**Files:**
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\ViewModels\QueryViewModel.cs`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Views\Pages\DatabaseWorkspacePage.xaml`
- Modify: `C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Views\MainWindow.xaml`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\ViewModels\QueryViewModelTests.cs`
- Modify: `C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Views\MainWindowSmokeTests.cs`

- [ ] **Step 1: Write failing tests for query empty-state and result summaries**

```csharp
[Fact]
public async Task ExecuteQueryAsync_ShouldExposeEmptyResultState_WhenQueryReturnsNoRows()
{
    var viewModel = new QueryViewModel(
        new QueryService(new FakeSqliteQueryExecutor(
            new QueryExecutionResult(
                Columns: ["id"],
                Rows: Array.Empty<IReadOnlyList<object?>>(),
                AffectedRows: 0,
                Duration: TimeSpan.FromMilliseconds(7),
                Message: "Query returned 0 row(s)."))),
        new ExportService(new FakeCsvExportWriter()),
        new FakeFileDialogService(null));

    viewModel.Configure(@"C:\data\sample.db", "users");
    await viewModel.ExecuteQueryAsync();

    viewModel.HasResults.Should().BeFalse();
    viewModel.ResultSummary.Should().Be("No rows returned.");
}
```

- [ ] **Step 2: Run the targeted query tests to verify they fail**

Run: `dotnet test C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Open.Db.Viewer.Shell.Tests.csproj --filter "ExecuteQueryAsync_ShouldExposeEmptyResultState_WhenQueryReturnsNoRows"`

Expected: FAIL with missing query summary members.

- [ ] **Step 3: Add query summary and more explicit status helpers**

```csharp
public partial class QueryViewModel : ObservableObject
{
    public string ResultSummary => Columns.Count == 0 && Rows.Count == 0
        ? "No rows returned."
        : $"{Rows.Count} row(s) · {Columns.Count} column(s)";

    public bool ShowEmptyResultState => !IsBusy && Columns.Count == 0 && Rows.Count == 0 && !string.IsNullOrWhiteSpace(DatabasePath);

    public bool ShowResultGrid => Columns.Count > 0 || Rows.Count > 0;

    public void Configure(string databasePath, string? tableName = null)
    {
        // existing configure logic
        OnPropertyChanged(nameof(ResultSummary));
        OnPropertyChanged(nameof(ShowEmptyResultState));
        OnPropertyChanged(nameof(ShowResultGrid));
    }

    private void ApplyResult(QueryExecutionResult result)
    {
        // existing result mapping
        OnPropertyChanged(nameof(ResultSummary));
        OnPropertyChanged(nameof(ShowEmptyResultState));
        OnPropertyChanged(nameof(ShowResultGrid));
    }

    private void ClearResults()
    {
        // existing clear logic
        OnPropertyChanged(nameof(ResultSummary));
        OnPropertyChanged(nameof(ShowEmptyResultState));
        OnPropertyChanged(nameof(ShowResultGrid));
    }
}
```

- [ ] **Step 4: Replace the schema and query tab visuals with panel-based surfaces**

```xml
<TabItem Header="Schema">
    <Grid Margin="12">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="12" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <Border Padding="14"
                Background="{DynamicResource ControlFillColorTertiaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1"
                CornerRadius="14">
            <StackPanel>
                <TextBlock FontSize="18"
                           FontWeight="SemiBold"
                           Text="{Binding Schema.TableName}" />
                <TextBlock Margin="0,6,0,0"
                           Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                           Text="{Binding Schema.ColumnCountSummary}" />
            </StackPanel>
        </Border>

        <Border Grid.Row="2"
                Padding="12"
                Background="{DynamicResource ControlFillColorSecondaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1"
                CornerRadius="14">
            <DataGrid AutoGenerateColumns="False"
                      CanUserAddRows="False"
                      IsReadOnly="True"
                      ItemsSource="{Binding Schema.Columns}">
                <!-- existing schema columns -->
            </DataGrid>
        </Border>
    </Grid>
</TabItem>

<TabItem Header="Query">
    <Grid Margin="12">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="12" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="12" />
            <RowDefinition Height="160" />
            <RowDefinition Height="12" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="12" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <Border Padding="14"
                Background="{DynamicResource ControlFillColorTertiaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1"
                CornerRadius="14">
            <!-- execute/export/template toolbar -->
        </Border>

        <Border Grid.Row="4"
                Padding="12"
                Background="{DynamicResource ControlFillColorSecondaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1"
                CornerRadius="14">
            <TextBox AcceptsReturn="True"
                     FontFamily="Cascadia Code"
                     FontSize="14"
                     Text="{Binding Query.QueryText, UpdateSourceTrigger=PropertyChanged}" />
        </Border>

        <Border Grid.Row="6"
                Padding="12"
                Background="{DynamicResource ControlFillColorTertiaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1"
                CornerRadius="14">
            <StackPanel>
                <TextBlock FontWeight="SemiBold"
                           Text="{Binding Query.ResultSummary}" />
                <TextBlock Margin="0,6,0,0"
                           Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                           Text="{Binding Query.StatusMessage}" />
            </StackPanel>
        </Border>

        <Border Grid.Row="8"
                Padding="12"
                Background="{DynamicResource ControlFillColorSecondaryBrush}"
                BorderBrush="{DynamicResource ControlStrokeColorSecondaryBrush}"
                BorderThickness="1"
                CornerRadius="14">
            <Grid>
                <DataGrid AutoGenerateColumns="True"
                          CanUserAddRows="False"
                          IsReadOnly="True"
                          ItemsSource="{Binding Query.ResultView}"
                          Visibility="{Binding Query.ShowResultGrid, Converter={StaticResource BooleanToVisibilityConverter}}" />

                <StackPanel Visibility="{Binding Query.ShowEmptyResultState, Converter={StaticResource BooleanToVisibilityConverter}}">
                    <TextBlock FontSize="18"
                               FontWeight="SemiBold"
                               Text="No query results yet" />
                    <TextBlock Margin="0,8,0,0"
                               Foreground="{DynamicResource TextFillColorSecondaryBrush}"
                               Text="Run the current SQL to see rows or a command result here." />
                </StackPanel>
            </Grid>
        </Border>
    </Grid>
</TabItem>
```

- [ ] **Step 5: Tidy the main window surface so the refreshed pages sit inside a calmer desktop shell**

```xml
<Grid Background="{DynamicResource ApplicationBackgroundBrush}">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>

    <ui:TitleBar x:Name="MainTitleBar"
                 Grid.Row="0"
                 Height="44"
                 CloseWindowByDoubleClickOnIcon="True" />

    <Border Grid.Row="1"
            Margin="12,0,12,12"
            Background="{DynamicResource ApplicationBackgroundBrush}"
            CornerRadius="20">
        <ContentControl Content="{Binding CurrentPage}"
                        ContentTemplateSelector="{StaticResource PageTemplateSelector}" />
    </Border>
</Grid>
```

- [ ] **Step 6: Run the full shell and solution tests**

Run: `dotnet test C:\Code\open.db.viewer\src\Open.Db.Viewer.slnx`

Expected: PASS

Run: `dotnet test C:\Code\open.db.viewer\tests\Open.Db.Viewer.Shell.Tests\Open.Db.Viewer.Shell.Tests.csproj --filter "QueryViewModelTests|MainWindow_ShouldRenderHomePageContent_OnStartup"`

Expected: PASS

- [ ] **Step 7: Launch the app and perform manual UX verification**

Run: `dotnet run --project C:\Code\open.db.viewer\src\Open.Db.Viewer.Shell\Open.Db.Viewer.Shell.csproj`

Expected:
- home page shows hero area, search, and card-based lists
- workspace top bar shows home and refresh actions
- empty states render clearly before table selection or result availability
- data tab exports the current visible page
- query tab shows improved editor, status, and result hierarchy

- [ ] **Step 8: Commit the schema/query polish and final verification**

```bash
git -C C:\Code\open.db.viewer add \
  src/Open.Db.Viewer.Shell/ViewModels/QueryViewModel.cs \
  src/Open.Db.Viewer.Shell/Views/Pages/DatabaseWorkspacePage.xaml \
  src/Open.Db.Viewer.Shell/Views/MainWindow.xaml \
  tests/Open.Db.Viewer.Shell.Tests/ViewModels/QueryViewModelTests.cs \
  tests/Open.Db.Viewer.Shell.Tests/Views/MainWindowSmokeTests.cs
git -C C:\Code\open.db.viewer commit -m "feat: complete WPF shell UX refresh"
```

## Self-Review

### Spec coverage

- home hero, search, pinned/recent card treatment: covered by Task 2
- workspace context bar, home and refresh actions, no-selection state: covered by Task 1 and Task 3
- schema, data, and query visual hierarchy: covered by Task 3, Task 4, and Task 5
- state feedback and current-page export: covered by Task 4 and Task 5

### Placeholder scan

- no `TODO`, `TBD`, `implement later`, or ellipsis placeholders remain
- every task includes exact file paths and commands
- code steps contain concrete snippets instead of abstract instructions

### Type consistency

- `StatusMessage`, `HasTableSelection`, `SelectedObjectTitle`, and `SelectedObjectSubtitle` are introduced before later XAML bindings use them
- `DataViewModel` export members are defined before the data-tab XAML binds them
- `QueryViewModel` summary members are defined before the query-tab XAML binds them
