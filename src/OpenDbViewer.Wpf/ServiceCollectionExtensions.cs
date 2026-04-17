using Microsoft.Extensions.DependencyInjection;
using OpenDbViewer.Application.Abstractions;
using OpenDbViewer.Application.Services;
using OpenDbViewer.Domain.Models;
using OpenDbViewer.Infrastructure.Sqlite.Export;
using OpenDbViewer.Infrastructure.Sqlite.Sqlite;
using OpenDbViewer.Shell.Services;
using OpenDbViewer.Shell.ViewModels;
using System.IO;

namespace OpenDbViewer.ShellHost;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOpenDbViewerWpfServices(this IServiceCollection services)
    {
        services.AddSingleton<Func<string, Task<bool>>>(_ => path => Task.FromResult(File.Exists(path)));

        services.AddSingleton<IDatabaseEntryRepository, InMemoryDatabaseEntryRepository>();
        services.AddSingleton<DatabaseEntryService>();
        services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
        services.AddSingleton<SqliteDatabaseInspector>();
        services.AddSingleton<SqliteTableDataReader>();
        services.AddSingleton<ISqliteQueryExecutor, SqliteQueryExecutor>();
        services.AddSingleton<ICsvExportWriter, CsvExportWriter>();
        services.AddSingleton<QueryService>();
        services.AddSingleton<ExportService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<ObjectExplorerViewModel>();
        services.AddSingleton<SchemaViewModel>();
        services.AddSingleton<DataViewModel>();
        services.AddSingleton<QueryViewModel>();
        services.AddSingleton<DatabaseWorkspaceViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<OpenDbViewer.Shell.Views.MainWindow>();

        return services;
    }

    private sealed class InMemoryDatabaseEntryRepository : IDatabaseEntryRepository
    {
        private readonly List<DatabaseEntry> _recentEntries = new();
        private readonly List<DatabaseEntry> _pinnedEntries = new();

        public Task<IReadOnlyList<DatabaseEntry>> GetRecentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseEntry>>(_recentEntries.ToArray());

        public Task<IReadOnlyList<DatabaseEntry>> GetPinnedAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DatabaseEntry>>(_pinnedEntries.ToArray());

        public Task SaveRecentAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
        {
            var existingIndex = _recentEntries.FindIndex(x => x.FilePath.Equals(entry.FilePath, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                _recentEntries[existingIndex] = entry;
            }
            else
            {
                _recentEntries.Add(entry);
            }

            return Task.CompletedTask;
        }

        public Task SavePinnedAsync(DatabaseEntry entry, CancellationToken cancellationToken = default)
        {
            var pinnedEntry = entry with { IsPinned = true };
            var existingIndex = _pinnedEntries.FindIndex(x => x.FilePath.Equals(entry.FilePath, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                _pinnedEntries[existingIndex] = pinnedEntry;
            }
            else
            {
                _pinnedEntries.Add(pinnedEntry);
            }

            return Task.CompletedTask;
        }

        public Task RemovePinnedAsync(Guid id, CancellationToken cancellationToken = default)
        {
            _pinnedEntries.RemoveAll(entry => entry.Id == id);
            return Task.CompletedTask;
        }
    }
}
