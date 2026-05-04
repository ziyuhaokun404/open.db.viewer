using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.Shell.ViewModels.Navigation;
using Open.Db.Viewer.ShellHost.Services;
using Open.Db.Viewer.ShellHost.ViewModels.Navigation;
using Open.Db.Viewer.ShellHost.ViewModels.Shell;
using Open.Db.Viewer.ShellHost.ViewModels.Workspace;

using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;

using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Open.Db.Viewer.Shell.Views;

public partial class MainWindow : FluentWindow
{
    private const string LogoAppName = "Open.Db.Viewer.Shell";
    private static readonly MethodInfo? SvgGetImageMethod = typeof(SvgImageExtension)
        .BaseType?
        .GetMethod("GetImage", BindingFlags.Instance | BindingFlags.NonPublic);
    private readonly ThemeService _themeService;

    public MainWindow(ViewModels.ShellViewModel viewModel, ThemeService themeService)
    {
        InitializeComponent();
        _themeService = themeService;
        DataContext = viewModel;
        viewModel.PropertyChanged += OnShellViewModelPropertyChanged;
        UpdateLogoVisual();
        UpdateThemeToggleVisual(_themeService.EffectiveTheme);
        UpdateNavigationSelection();
        _themeService.ThemeChanged += OnThemeChanged;
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
        _themeService.ToggleTheme();
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateLogoVisual();
        UpdateThemeToggleVisual(_themeService.EffectiveTheme);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is ShellViewModel shell)
        {
            shell.PropertyChanged -= OnShellViewModelPropertyChanged;
        }

        _themeService.ThemeChanged -= OnThemeChanged;
        Closed -= OnClosed;
    }

    private void OnShellViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ShellViewModel.CurrentSection) or null or "")
        {
            UpdateNavigationSelection();
        }
    }

    private void UpdateThemeToggleVisual(Wpf.Ui.Appearance.ApplicationTheme applicationTheme)
    {
        if (ThemeToggleButton is null || ThemeToggleIcon is null)
        {
            return;
        }

        var isDarkTheme = applicationTheme == ApplicationTheme.Dark;
        ThemeToggleIcon.Symbol = isDarkTheme ? SymbolRegular.WeatherSunny24 : SymbolRegular.WeatherMoon24;
        ThemeToggleButton.ToolTip = isDarkTheme ? "切换到浅色模式" : "切换到深色模式";
    }

    private void UpdateLogoVisual()
    {
        var logoPath = TryFindResource("AppLogoSvgPath") as string
            ?? (_themeService.EffectiveTheme == ApplicationTheme.Dark
                ? "/Assets/Icons/open_db_viewer_logo_light.svg"
                : "/Assets/Icons/open_db_viewer_logo_transparent.svg");

        if (AppIconImage is not null)
        {
            AppIconImage.Source = CreateLogoResourceUri(logoPath);
        }

        Icon = CreateWindowIcon(logoPath);
    }

    private static ImageSource? CreateWindowIcon(string logoPath)
    {
        var logoUri = CreateLogoResourceUri(logoPath);

        if (SvgGetImageMethod is not null)
        {
            var svgImage = new SvgImageExtension
            {
                AppName = LogoAppName
            };

            if (SvgGetImageMethod.Invoke(svgImage, [logoUri]) is ImageSource imageSource)
            {
                return imageSource;
            }
        }

        try
        {
            var drawingSettings = new WpfDrawingSettings();
            var svgReader = new FileSvgReader(drawingSettings, false);
            if (svgReader.Read(logoUri) is DrawingGroup drawingGroup)
            {
                var drawingImage = new DrawingImage(drawingGroup);
                if (drawingImage.CanFreeze)
                {
                    drawingImage.Freeze();
                }

                return drawingImage;
            }
        }
        catch
        {
            // Keep a non-null icon so window initialization stays resilient in tests and at runtime.
        }

        return new DrawingImage();
    }

    private static Uri CreateLogoResourceUri(string logoPath)
    {
        return new Uri($"pack://application:,,,/{LogoAppName};component{logoPath}", UriKind.Absolute);
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
