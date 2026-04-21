using Microsoft.Win32;

namespace Open.Db.Viewer.Shell.Services;

public interface IFileDialogService
{
    string? PickSqliteFile();

    string? PickCsvSavePath(string suggestedFileName);
}

public sealed class FileDialogService : IFileDialogService
{
    public string? PickSqliteFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "打开 SQLite 数据库",
            Filter = "SQLite 数据库 (*.db;*.sqlite;*.sqlite3)|*.db;*.sqlite;*.sqlite3|所有文件 (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickCsvSavePath(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            Title = "导出 CSV",
            Filter = "CSV 文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
            FileName = suggestedFileName,
            AddExtension = true,
            DefaultExt = ".csv",
            OverwritePrompt = true
        };

        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
