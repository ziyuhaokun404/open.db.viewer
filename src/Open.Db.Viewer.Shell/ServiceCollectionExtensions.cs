using Microsoft.Extensions.DependencyInjection;
using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Infrastructure.Sqlite.Export;
using Open.Db.Viewer.Infrastructure.Sqlite.Sqlite;
using Open.Db.Viewer.Infrastructure.Sqlite.Storage;
using Open.Db.Viewer.Shell.Services;
using Open.Db.Viewer.Shell.ViewModels;
using Open.Db.Viewer.Shell.ViewModels.Navigation;
using Open.Db.Viewer.Shell.ViewModels.Workspace;
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
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<HomeLandingViewModel>();
        services.AddSingleton<RecentDatabasesViewModel>();
        services.AddSingleton<PinnedDatabasesViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<AboutViewModel>();
        services.AddSingleton<WorkspaceHostViewModel>();
        services.AddSingleton<ObjectExplorerViewModel>();
        services.AddSingleton<SchemaViewModel>();
        services.AddSingleton<DataViewModel>(provider =>
            new DataViewModel(
                provider.GetRequiredService<SqliteTableDataReader>(),
                provider.GetRequiredService<ExportService>(),
                provider.GetRequiredService<IFileDialogService>()));
        services.AddSingleton<QueryViewModel>();
        services.AddSingleton<DatabaseWorkspaceViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<Open.Db.Viewer.Shell.Views.MainWindow>();

        return services;
    }
}
