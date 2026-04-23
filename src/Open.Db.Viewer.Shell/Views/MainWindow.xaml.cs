using Wpf.Ui.Controls;
using System.Windows;
using System.Windows.Controls;
using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.Shell.ViewModels.Navigation;
using Open.Db.Viewer.Shell.ViewModels.Workspace;
using Wpf.Ui.Appearance;

namespace Open.Db.Viewer.Shell.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow(ViewModels.ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        UpdateThemeToggleVisual(ApplicationThemeManager.GetAppTheme());
        ApplicationThemeManager.Changed += OnApplicationThemeChanged;
        Closed += OnClosed;
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
        ApplicationThemeManager.Changed -= OnApplicationThemeChanged;
        Closed -= OnClosed;
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
