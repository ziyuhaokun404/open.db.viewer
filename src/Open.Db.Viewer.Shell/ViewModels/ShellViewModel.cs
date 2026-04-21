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
