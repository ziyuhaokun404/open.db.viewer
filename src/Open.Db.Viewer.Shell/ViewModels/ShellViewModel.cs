using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Shell.ViewModels.Navigation;
using Open.Db.Viewer.Shell.ViewModels.Shell;
using Open.Db.Viewer.Shell.ViewModels.Workspace;
using System.Collections.ObjectModel;

namespace Open.Db.Viewer.Shell.ViewModels;

public sealed partial class ShellViewModel : ObservableObject
{
    [ObservableProperty]
    private ShellSection currentSection = ShellSection.Home;

    [ObservableProperty]
    private object currentContentViewModel;

    [ObservableProperty]
    private string currentDatabasePath = string.Empty;

    public ShellViewModel(
        DatabaseWorkspaceViewModel databaseWorkspaceViewModel,
        HomeLandingViewModel homeLandingViewModel,
        RecentDatabasesViewModel recentDatabasesViewModel,
        PinnedDatabasesViewModel pinnedDatabasesViewModel,
        SettingsViewModel settingsViewModel,
        AboutViewModel aboutViewModel)
    {
        Workspace = databaseWorkspaceViewModel;
        HomeLanding = homeLandingViewModel;
        RecentPage = recentDatabasesViewModel;
        PinnedPage = pinnedDatabasesViewModel;
        SettingsPage = settingsViewModel;
        AboutPage = aboutViewModel;
        WorkspaceHost = new WorkspaceHostViewModel(Workspace);

        NavigationItems =
        [
            new ShellNavigationItem(ShellSection.Home, "首页"),
            new ShellNavigationItem(ShellSection.Recent, "最近使用"),
            new ShellNavigationItem(ShellSection.Pinned, "已固定"),
            new ShellNavigationItem(ShellSection.Workspace, "数据库工作台")
        ];

        CurrentContentViewModel = HomeLanding;
        UpdateNavigationSelection();

        HomeLanding.DatabaseOpenedAsync = OpenWorkspaceAsync;
        RecentPage.DatabaseOpenedAsync = OpenWorkspaceAsync;
        PinnedPage.DatabaseOpenedAsync = OpenWorkspaceAsync;
        Workspace.RequestReturnHomeAsync = ReturnHomeAsync;

        _ = HomeLanding.LoadAsync();
    }

    public ObservableCollection<ShellNavigationItem> NavigationItems { get; }

    public HomeLandingViewModel HomeLanding { get; }

    public RecentDatabasesViewModel RecentPage { get; }

    public PinnedDatabasesViewModel PinnedPage { get; }

    public SettingsViewModel SettingsPage { get; }

    public AboutViewModel AboutPage { get; }

    public DatabaseWorkspaceViewModel Workspace { get; }

    public WorkspaceHostViewModel WorkspaceHost { get; }

    [RelayCommand]
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
        _ = LoadCurrentSectionAsync();
    }

    [RelayCommand]
    public Task OpenDatabaseAsync(CancellationToken cancellationToken = default) =>
        HomeLanding.OpenDatabaseAsync(cancellationToken);

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
        await Workspace.LoadAsync(databasePath, cancellationToken);
        await HomeLanding.LoadAsync(cancellationToken);
        await RecentPage.LoadAsync(cancellationToken);
        await PinnedPage.LoadAsync(cancellationToken);
        CurrentDatabasePath = databasePath;
        NavigateToSection(ShellSection.Workspace);
    }

    private Task ReturnHomeAsync()
    {
        NavigateToSection(ShellSection.Home);
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
