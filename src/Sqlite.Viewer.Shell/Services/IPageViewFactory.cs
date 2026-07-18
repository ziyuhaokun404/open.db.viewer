using System.Windows;

namespace Sqlite.Viewer.ShellHost.Services;

public interface IPageViewFactory
{
    FrameworkElement CreateView(object viewModel);
}
