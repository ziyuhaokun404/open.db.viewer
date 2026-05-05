using System.Windows;

namespace Open.Db.Viewer.ShellHost.Services;

public sealed class DialogService : IDialogService
{
    public bool Confirm(string title, string message) =>
        MessageBox.Show(
            message,
            title,
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning,
            MessageBoxResult.Cancel) == MessageBoxResult.OK;
}
