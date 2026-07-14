using System.Windows;

namespace Open.Db.Viewer.ShellHost.Services;

public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Clipboard.SetText(text);
    }
}
