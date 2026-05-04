using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.ShellHost.ViewModels.Navigation;

using System.Collections.ObjectModel;

namespace Open.Db.Viewer.Shell.ViewModels.Navigation;

public sealed partial class PinnedDatabasesViewModel : ObservableObject
{
    private readonly DatabaseEntryService _databaseEntryService;

    public PinnedDatabasesViewModel(DatabaseEntryService databaseEntryService)
    {
        ArgumentNullException.ThrowIfNull(databaseEntryService);
        _databaseEntryService = databaseEntryService;
    }

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string statusMessage = StatusMessages.DefaultReady;

    public ObservableCollection<DatabaseEntry> Entries { get; } = new();

    public ObservableCollection<DatabaseEntry> FilteredEntries { get; } = new();

    public Func<string, CancellationToken, Task>? DatabaseOpenedAsync { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var pinnedEntries = await _databaseEntryService.GetPinnedAsync(cancellationToken);

        Entries.Clear();
        foreach (var entry in pinnedEntries.OrderByDescending(item => item.LastOpenedAt))
        {
            Entries.Add(entry);
        }

        ApplyFilter();
    }

    [RelayCommand]
    public async Task OpenEntryAsync(DatabaseEntry? entry, CancellationToken cancellationToken = default)
    {
        if (entry is null)
        {
            return;
        }

        var result = await _databaseEntryService.OpenAsync(entry.FilePath, cancellationToken);
        StatusMessage = result.Message;

        if (result.IsSuccess)
        {
            await LoadAsync(cancellationToken);
            if (DatabaseOpenedAsync is not null)
            {
                await DatabaseOpenedAsync(entry.FilePath, cancellationToken);
            }
        }
    }

    [RelayCommand]
    public async Task TogglePinAsync(DatabaseEntry? entry, CancellationToken cancellationToken = default)
    {
        if (entry is null)
        {
            return;
        }

        await _databaseEntryService.UnpinAsync(entry, cancellationToken);
        await LoadAsync(cancellationToken);
        StatusMessage = $"已取消固定 {entry.Name}。";
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? Entries
            : Entries.Where(entry =>
                entry.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                entry.FilePath.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        FilteredEntries.Clear();
        foreach (var entry in filtered)
        {
            FilteredEntries.Add(entry);
        }
    }
}
