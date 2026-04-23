using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.Shell.ViewModels.Navigation;
using Open.Db.Viewer.Shell.ViewModels.Shell;
using Open.Db.Viewer.Shell.ViewModels.Workspace;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Open.Db.Viewer.Shell.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow(ViewModels.ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += OnShellViewModelPropertyChanged;
        UpdateThemeToggleVisual(ApplicationThemeManager.GetAppTheme());
        UpdateNavigationSelection();
        ApplicationThemeManager.Changed += OnApplicationThemeChanged;
        Closed += OnClosed;
    }

    private void NavItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ShellViewModel shell || sender is not NavigationViewItem item || item.Tag is not string routeKey)
        {
            return;
        }

        var section = routeKey switch
        {
            "home" => ShellSection.Home,
            "recent" => ShellSection.Recent,
            "pinned" => ShellSection.Pinned,
            "workspace" => ShellSection.Workspace,
            "settings" => ShellSection.Settings,
            "about" => ShellSection.About,
            _ => (ShellSection?)null
        };

        if (section is ShellSection targetSection)
        {
            shell.NavigateToSection(targetSection);
        }
    }

    private void ToggleThemeMode(object sender, RoutedEventArgs e)
    {
        var nextTheme = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark
            ? ApplicationTheme.Light
            : ApplicationTheme.Dark;

        ApplicationThemeManager.Apply(nextTheme);
        UpdateThemeToggleVisual(nextTheme);
    }

    private void OnApplicationThemeChanged(ApplicationTheme applicationTheme, System.Windows.Media.Color systemAccent)
    {
        UpdateThemeToggleVisual(applicationTheme);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is ShellViewModel shell)
        {
            shell.PropertyChanged -= OnShellViewModelPropertyChanged;
        }

        ApplicationThemeManager.Changed -= OnApplicationThemeChanged;
        Closed -= OnClosed;
    }

    private void OnShellViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ShellViewModel.CurrentSection) or null or "")
        {
            UpdateNavigationSelection();
        }
    }

    private void UpdateThemeToggleVisual(ApplicationTheme applicationTheme)
    {
        if (ThemeToggleButton is null || ThemeToggleIcon is null)
        {
            return;
        }

        var isDarkTheme = applicationTheme == ApplicationTheme.Dark;
        ThemeToggleIcon.Symbol = isDarkTheme ? SymbolRegular.WeatherMoon24 : SymbolRegular.WeatherSunny24;
        ThemeToggleButton.ToolTip = isDarkTheme ? "当前为深色模式" : "当前为浅色模式";
    }

    private void UpdateNavigationSelection()
    {
        if (DataContext is not ShellViewModel shell || RootNavigation is null)
        {
            return;
        }

        HomeNavItem.IsActive = shell.CurrentSection == ShellSection.Home;
        RecentNavItem.IsActive = shell.CurrentSection == ShellSection.Recent;
        PinnedNavItem.IsActive = shell.CurrentSection == ShellSection.Pinned;
        WorkspaceNavItem.IsActive = shell.CurrentSection == ShellSection.Workspace;
        SettingsNavItem.IsActive = shell.CurrentSection == ShellSection.Settings;
        AboutNavItem.IsActive = shell.CurrentSection == ShellSection.About;

    }
}

public sealed class PageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? HomeTemplate { get; set; }

    public DataTemplate? RecentTemplate { get; set; }

    public DataTemplate? PinnedTemplate { get; set; }

    public DataTemplate? SettingsTemplate { get; set; }

    public DataTemplate? AboutTemplate { get; set; }

    public DataTemplate? WorkspaceTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container)
    {
        return item switch
        {
            HomeLandingViewModel => HomeTemplate,
            RecentDatabasesViewModel => RecentTemplate,
            PinnedDatabasesViewModel => PinnedTemplate,
            SettingsViewModel => SettingsTemplate,
            AboutViewModel => AboutTemplate,
            WorkspaceHostViewModel => WorkspaceTemplate,
            _ => base.SelectTemplate(item, container)
        };
    }
}
