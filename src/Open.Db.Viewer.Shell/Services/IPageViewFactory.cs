using System.Windows;

namespace Open.Db.Viewer.ShellHost.Services;

public interface IPageViewFactory
{
    FrameworkElement CreateView(object viewModel);
}
