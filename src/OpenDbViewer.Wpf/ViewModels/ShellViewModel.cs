using CommunityToolkit.Mvvm.ComponentModel;

namespace OpenDbViewer.Shell.ViewModels;

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
    }

    public HomeViewModel Home { get; }

    public DatabaseWorkspaceViewModel Workspace { get; }

    private async Task OpenWorkspaceAsync(string databasePath, CancellationToken cancellationToken)
    {
        await Workspace.LoadAsync(databasePath, cancellationToken);
        CurrentPage = Workspace;
    }
}
