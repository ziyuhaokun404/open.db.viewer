using Open.Db.Viewer.Shell.ViewModels;

namespace Open.Db.Viewer.ShellHost.ViewModels.Workspace;

public sealed class WorkspaceHostViewModel
{
    public WorkspaceHostViewModel(DatabaseWorkspaceViewModel workspace)
    {
        Workspace = workspace;
    }

    public DatabaseWorkspaceViewModel Workspace { get; }
}
