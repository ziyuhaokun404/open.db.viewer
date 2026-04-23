using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using FluentAssertions;
using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.Services;
using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.Shell.ViewModels.Navigation;
using Open.Db.Viewer.Shell.Views;
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
                var databaseEntryService = new DatabaseEntryService(repository, _ => Task.FromResult(true));
                var home = new HomeViewModel(databaseEntryService, new FakeFileDialogService());
                var workspace = new DatabaseWorkspaceViewModel(
                    new ObjectExplorerViewModel(),
                    new SchemaViewModel(),
                    new DataViewModel(),
                    new QueryViewModel(
                        new QueryService(new NoopSqliteQueryExecutor()),
                        new ExportService(new NoopCsvExportWriter()),
                        new FakeFileDialogService()));
                var shell = new ShellViewModel(
                    home,
                    workspace,
                    new HomeLandingViewModel(databaseEntryService, new FakeFileDialogService()),
                    new RecentDatabasesViewModel(databaseEntryService),
                    new PinnedDatabasesViewModel(databaseEntryService),
                    new SettingsViewModel(),
                    new AboutViewModel());
                var window = new MainWindow(shell);

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
                themeToggleButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                DoEvents();
                ApplicationThemeManager.GetAppTheme().Should().Be(ApplicationTheme.Dark);

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

                renderedTexts.Should().Contain("数据库查看器");
                renderedTexts.Should().Contain("首页");
                renderedTexts.Should().Contain("最近使用");
                renderedTexts.Should().Contain("数据库工作台");
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
        public Task<IReadOnlyList<DatabaseEntry>> GetRecentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseEntry>>(Array.Empty<DatabaseEntry>());

        public Task<IReadOnlyList<DatabaseEntry>> GetPinnedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseEntry>>(Array.Empty<DatabaseEntry>());

        public Task SaveRecentAsync(DatabaseEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SavePinnedAsync(DatabaseEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemovePinnedAsync(Guid id, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
