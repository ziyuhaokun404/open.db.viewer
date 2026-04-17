using Microsoft.Win32;

namespace OpenDbViewer.Shell.Services;

public interface IFileDialogService
{
    string? PickSqliteFile();
}

public sealed class FileDialogService : IFileDialogService
{
    public string? PickSqliteFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open SQLite database",
            Filter = "SQLite databases (*.db;*.sqlite;*.sqlite3)|*.db;*.sqlite;*.sqlite3|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
