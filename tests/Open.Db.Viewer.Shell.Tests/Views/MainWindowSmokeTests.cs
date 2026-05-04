using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

using FluentAssertions;

using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.Shell.ViewModels.Navigation;
using Open.Db.Viewer.Shell.Views;
using Open.Db.Viewer.ShellHost.Services;
using Open.Db.Viewer.ShellHost.ViewModels.Shell;

using Wpf.Ui.Appearance;

using WpfUiControls = Wpf.Ui.Controls;

namespace Open.Db.Viewer.Shell.Tests.Views;

public class MainWindowSmokeTests
{
    [Fact]
    public void MainWindow_ShouldRenderHomePageContent_OnStartup()
    {
        Exception? failure = null;

        var thread = new Thread(() =>
        {
            try
            {
                var application = EnsureApplicationResources();
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                var repository = new InMemoryDatabaseEntryRepository();
                repository.SeedRecent(new DatabaseEntry(
                    Guid.NewGuid(),
                    "app",
                    @"C:\data\demo\app.db",
                    new DateTimeOffset(2026, 4, 23, 1, 58, 0, TimeSpan.Zero),
                    false));
                repository.SeedPinned(new DatabaseEntry(
                    Guid.NewGuid(),
                    "northwind",
                    @"C:\data\demo\northwind.db",
                    new DateTimeOffset(2026, 4, 23, 1, 30, 0, TimeSpan.Zero),
                    true));
                var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
                var workspace = new DatabaseWorkspaceViewModel(
                    new ObjectExplorerViewModel(),
                    new SchemaViewModel(),
                    new DataViewModel(),
                    new QueryViewModel(
                        new QueryService(new NoopSqliteQueryExecutor()),
                        new ExportService(new NoopCsvExportWriter()),
                        new FakeFileDialogService()));
                var shell = new ShellViewModel(
                    workspace,
                    new HomeLandingViewModel(databaseEntryService, new FakeFileDialogService()),
                    new SettingsViewModel(),
                    new AboutViewModel());
                var themeService = new ThemeService();
                themeService.ApplyPreference(ThemePreference.Light);
                var window = new MainWindow(shell, themeService);

                window.Show();
                window.ApplyTemplate();
                window.UpdateLayout();
                DoEvents();

                var rootGrid = window.Content.Should().BeOfType<Grid>().Subject;
                rootGrid.Children.Count.Should().BeGreaterThan(0);
                rootGrid.ActualWidth.Should().BeGreaterThan(0);
                rootGrid.ActualHeight.Should().BeGreaterThan(0);

                window.ExtendsContentIntoTitleBar.Should().BeTrue();
                var titleBar = window.FindName("MainTitleBar").Should().BeOfType<WpfUiControls.TitleBar>().Subject;
                titleBar.ActualHeight.Should().BeGreaterThan(0);
                window.Icon.Should().NotBeNull();

                var appIcon = window.FindName("AppIconImage");
                appIcon.Should().NotBeNull();
                var appIconElement = appIcon.Should().BeAssignableTo<FrameworkElement>().Subject;
                appIconElement.ActualWidth.Should().BeGreaterThan(0);
                appIconElement.ActualHeight.Should().BeGreaterThan(0);

                var themeToggleButton = window.FindName("ThemeToggleButton").Should().BeOfType<WpfUiControls.Button>().Subject;
                themeToggleButton.ToolTip.Should().Be("切换到深色模式");
                themeToggleButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                DoEvents();
                ApplicationThemeManager.GetAppTheme().Should().Be(ApplicationTheme.Dark);
                themeToggleButton.ToolTip.Should().Be("切换到浅色模式");

                var rootNavigation = window.FindName("RootNavigation").Should().BeOfType<WpfUiControls.NavigationView>().Subject;
                var homeNavItem = window.FindName("HomeNavItem").Should().BeOfType<WpfUiControls.NavigationViewItem>().Subject;
                var settingsNavItem = window.FindName("SettingsNavItem").Should().BeOfType<WpfUiControls.NavigationViewItem>().Subject;

                rootNavigation.MenuItems.Should().NotBeNull();
                homeNavItem.IsActive.Should().BeTrue();
                settingsNavItem.Content.Should().Be("设置");

                shell.NavigateToSection(ShellSection.Settings);
                DoEvents();
                homeNavItem.IsActive.Should().BeFalse();
                settingsNavItem.IsActive.Should().BeTrue();

                shell.NavigateToSection(ShellSection.Home);
                DoEvents();
                homeNavItem.IsActive.Should().BeTrue();

                var contentControl = EnumerateVisualTree(window)
                    .OfType<ContentControl>()
                    .Single(control => ReferenceEquals(control.Content, shell.CurrentContentViewModel));
                contentControl.Content.Should().BeSameAs(shell.CurrentContentViewModel);
                contentControl.ActualWidth.Should().BeGreaterThan(0);
                contentControl.ActualHeight.Should().BeGreaterThan(0);

                var renderedTexts = EnumerateVisualTree(window)
                    .OfType<System.Windows.Controls.TextBlock>()
                    .Select(node => node.Text)
                    .Where(text => !string.IsNullOrWhiteSpace(text))
                    .ToArray();

                renderedTexts.Should().Contain("Open.db.viewer");
                renderedTexts.Count(text => text == "Open.db.viewer").Should().Be(1);
                renderedTexts.Should().Contain("首页");
                renderedTexts.Should().Contain("数据库工作台");
                renderedTexts.Should().Contain("设置");
                renderedTexts.Should().Contain("关于");
                renderedTexts.Should().Contain("快速打开");
                renderedTexts.Should().Contain("查看全部");
                renderedTexts.Should().Contain(@"C:\data\demo\app.db");
                renderedTexts.Should().NotContain("轻量桌面工作台");
                renderedTexts.Should().NotContain("HOME");
                renderedTexts.Should().NotContain("已固定的数据库");
                renderedTexts.Should().NotContain(@"C:\data\demo\northwind.db");

                window.Close();
                themeService.Dispose();
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

    private static IEnumerable<DependencyObject> EnumerateVisualTree(DependencyObject root)
    {
        yield return root;

        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < childCount; index++)
        {
            foreach (var child in EnumerateVisualTree(VisualTreeHelper.GetChild(root, index)))
            {
                yield return child;
            }
        }
    }

    private static void DoEvents()
    {
        var frame = new System.Windows.Threading.DispatcherFrame();
        _ = System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => frame.Continue = false));
        System.Windows.Threading.Dispatcher.PushFrame(frame);
    }

    private static System.Windows.Application EnsureApplicationResources()
    {
        var application = System.Windows.Application.Current ?? new System.Windows.Application
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown
        };

        if (application.Resources.MergedDictionaries.Count == 0)
        {
            application.Resources.MergedDictionaries.Add(
                (ResourceDictionary)XamlReader.Parse(
                    """
                    <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                                        xmlns:ui="http://schemas.lepo.co/wpfui/2022/xaml">
                        <ResourceDictionary.MergedDictionaries>
                            <ui:ThemesDictionary Theme="Light" />
                            <ui:ControlsDictionary />
                        </ResourceDictionary.MergedDictionaries>
                    </ResourceDictionary>
                    """));
        }

        return application;
    }

    private sealed class FakeFileDialogService : IFileDialogService
    {
        public string? PickSqliteFile() => null;

        public string? PickCsvSavePath(string suggestedFileName) => null;
    }

    private sealed class NoopSqliteQueryExecutor : ISqliteQueryExecutor
    {
        public Task<QueryExecutionResult> ExecuteAsync(string filePath, string sql, CancellationToken cancellationToken = default) =>
            Task.FromResult(new QueryExecutionResult(Array.Empty<string>(), Array.Empty<IReadOnlyList<object?>>(), 0, TimeSpan.Zero, string.Empty));
    }

    private sealed class NoopCsvExportWriter : ICsvExportWriter
    {
        public Task WriteAsync(
            string filePath,
            IReadOnlyList<string> columns,
            IReadOnlyList<IReadOnlyList<object?>> rows,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class InMemoryDatabaseEntryRepository : IDatabaseEntryRepository
    {
        private readonly List<DatabaseEntry> _recentEntries = [];
        private readonly List<DatabaseEntry> _pinnedEntries = [];

        public void SeedRecent(DatabaseEntry entry) => _recentEntries.Add(entry);

        public void SeedPinned(DatabaseEntry entry) => _pinnedEntries.Add(entry);

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
            _pinnedEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task RemovePinnedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _pinnedEntries.RemoveAll(item => item.Id == id);
            return Task.CompletedTask;
        }
    }
}
