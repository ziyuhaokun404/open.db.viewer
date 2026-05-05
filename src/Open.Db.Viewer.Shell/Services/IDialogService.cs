namespace Open.Db.Viewer.ShellHost.Services;

public interface IDialogService
{
    bool Confirm(string title, string message);
}
