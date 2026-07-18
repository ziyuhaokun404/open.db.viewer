using Microsoft.Extensions.DependencyInjection;
using Sqlite.Viewer.Application.Abstractions;
using Sqlite.Viewer.ShellHost.Services;
using System.Windows;

namespace Sqlite.Viewer.ShellHost;

public partial class App : System.Windows.Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _serviceProvider = new ServiceCollection()
            .AddSqliteViewerWpfServices()
            .BuildServiceProvider();

        // Load settings before ThemeService.Initialize so theme preference is restored.
        _serviceProvider.GetRequiredService<IAppSettingsStore>().LoadAsync().GetAwaiter().GetResult();
        _serviceProvider.GetRequiredService<ThemeService>().Initialize();

        var mainWindow = _serviceProvider.GetRequiredService<Shell.Views.MainWindow>();
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
