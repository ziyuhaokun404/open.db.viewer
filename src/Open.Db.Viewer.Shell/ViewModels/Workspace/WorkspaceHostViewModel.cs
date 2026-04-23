namespace Open.Db.Viewer.Shell.ViewModels.Workspace;

public sealed class WorkspaceHostViewModel
{
    public WorkspaceHostViewModel(DatabaseWorkspaceViewModel workspace)
    {
        Workspace = workspace;
    }

    public DatabaseWorkspaceViewModel Workspace { get; }
}
