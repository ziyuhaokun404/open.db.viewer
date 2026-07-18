using Sqlite.Viewer.ShellHost.Services;

namespace Sqlite.Viewer.Shell.Tests.Support;

public sealed class NoopDialogService : IDialogService
{
    public bool Confirm(string title, string message) => true;
}
