using CommunityToolkit.Mvvm.ComponentModel;

namespace Open.Db.Viewer.Shell.ViewModels.Shell;

public sealed partial class ShellNavigationItem : ObservableObject
{
    public ShellNavigationItem(ShellSection section, string title)
    {
        Section = section;
        Title = title;
    }

    public ShellSection Section { get; }

    public string Title { get; }

    [ObservableProperty]
    private bool isSelected;
}
