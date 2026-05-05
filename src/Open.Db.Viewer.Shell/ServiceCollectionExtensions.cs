using Microsoft.Extensions.DependencyInjection;

using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Infrastructure.Sqlite.Export;
using Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;
using Open.Db.Viewer.Infrastructure.Sqlite.Storage;
using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.Shell.ViewModels.Navigation;
using Open.Db.Viewer.Shell.Views.Navigation;
using Open.Db.Viewer.Shell.Views.Workspace;
using Open.Db.Viewer.ShellHost.Services;
using Open.Db.Viewer.ShellHost.ViewModels.Navigation;

using System.IO;

namespace Open.Db.Viewer.ShellHost;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenDbViewerWpfServices(this IServiceCollection services)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenDbViewer");

        services.AddSingleton<Func<string, Task<bool>>>(_ => path => Task.FromResult(File.Exists(path)));

        services.AddSingleton<IDatabaseEntryRepository>(_ => new FileDatabaseEntryRepository(appDataPath));
        services.AddSingleton<ThemeService>();
        services.AddSingleton<DatabaseEntryService>();
        services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
        services.AddSingleton<SqliteDatabaseInspector>();
        services.AddSingleton<SqliteTableDataReader>();
        services.AddSingleton<ISqliteQueryExecutor, SqliteQueryExecutor>();
        services.AddSingleton<ICsvExportWriter, CsvExportWriter>();
        services.AddSingleton<QueryService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<HomeLandingViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AboutViewModel>();
        services.AddSingleton<ObjectExplorerViewModel>();
        services.AddSingleton<SchemaViewModel>();
        services.AddSingleton(provider =>
            new DataViewModel(
                provider.GetRequiredService<SqliteTableDataReader>(),
                provider.GetRequiredService<ExportService>(),
                provider.GetRequiredService<IFileDialogService>()));
        services.AddSingleton<QueryViewModel>();
        services.AddSingleton<DatabaseWorkspaceViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<IPageViewFactory>(provider => new PageViewFactory(
            provider,
            new Dictionary<Type, Type>
            {
                [typeof(HomeLandingViewModel)] = typeof(HomeLandingPage),
                [typeof(SettingsViewModel)] = typeof(SettingsPage),
                [typeof(AboutViewModel)] = typeof(AboutPage),
                [typeof(DatabaseWorkspaceViewModel)] = typeof(WorkspaceHostPage)
            }));
        services.AddTransient<HomeLandingPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<AboutPage>();
        services.AddTransient<WorkspaceHostPage>();
        services.AddSingleton<Shell.Views.MainWindow>();

        return services;
    }
}
