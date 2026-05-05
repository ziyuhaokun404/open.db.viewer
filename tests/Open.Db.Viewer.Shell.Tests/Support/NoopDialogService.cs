using Open.Db.Viewer.ShellHost.Services;

namespace Open.Db.Viewer.Shell.Tests.Support;

public sealed class NoopDialogService : IDialogService
{
    public bool Confirm(string title, string message) => true;
}
