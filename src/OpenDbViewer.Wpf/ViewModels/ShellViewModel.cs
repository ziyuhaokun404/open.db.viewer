namespace OpenDbViewer.Shell.ViewModels;

public sealed class ShellViewModel
{
    public ShellViewModel(HomeViewModel homeViewModel)
    {
        Home = homeViewModel;
    }

    public HomeViewModel Home { get; }

    public HomeViewModel CurrentPage => Home;
}
