using Microsoft.Extensions.DependencyInjection;

using Sqlite.Viewer.Application.Abstractions;
using Sqlite.Viewer.Application.Services;
using Sqlite.Viewer.Infrastructure.Sqlite.Export;
using Sqlite.Viewer.Infrastructure.Sqlite.Sqlite;
using Sqlite.Viewer.Infrastructure.Sqlite.Storage;
using Sqlite.Viewer.Shell.ViewModels;
using Sqlite.Viewer.Shell.ViewModels.Navigation;
using Sqlite.Viewer.Shell.Views.Navigation;
using Sqlite.Viewer.Shell.Views.Workspace;
using Sqlite.Viewer.ShellHost.Services;
using Sqlite.Viewer.ShellHost.ViewModels.Navigation;

using System.IO;

namespace Sqlite.Viewer.ShellHost;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteViewerWpfServices(this IServiceCollection services)
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SqliteViewer");

        services.AddSingleton<Func<string, Task<bool>>>(_ => path => Task.FromResult(File.Exists(path)));

        services.AddSingleton<IDatabaseEntryRepository>(_ => new FileDatabaseEntryRepository(appDataPath));
        services.AddSingleton<IAppSettingsStore>(_ => new FileAppSettingsStore(appDataPath));
        services.AddSingleton<IQueryHistoryStore>(_ => new FileQueryHistoryStore(appDataPath));
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<ThemeService>(provider =>
            new ThemeService(provider.GetRequiredService<IAppSettingsStore>()));
        services.AddSingleton<IThemeService>(provider => provider.GetRequiredService<ThemeService>());
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
                provider.GetRequiredService<IClipboardService>(),
                provider.GetRequiredService<ExportService>(),
                provider.GetRequiredService<IFileDialogService>(),
                provider.GetRequiredService<IAppSettingsStore>()));
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
