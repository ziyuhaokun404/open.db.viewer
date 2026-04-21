using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace Open.Db.Viewer.ShellHost;

public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _serviceProvider = new ServiceCollection()
            .AddOpenDbViewerWpfServices()
            .BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<Open.Db.Viewer.Shell.Views.MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        base.OnExit(e);
    }
}
