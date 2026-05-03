using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using System.Collections.ObjectModel;

namespace Open.Db.Viewer.Shell.ViewModels.Navigation;

public sealed partial class RecentDatabasesViewModel : ObservableObject
{
    private readonly DatabaseEntryService _databaseEntryService;

    public RecentDatabasesViewModel(DatabaseEntryService databaseEntryService)
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
        var recentEntries = await _databaseEntryService.GetRecentAsync(cancellationToken);

        Entries.Clear();
        foreach (var entry in recentEntries.OrderByDescending(item => item.LastOpenedAt))
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

        await _databaseEntryService.PinAsync(entry, cancellationToken);
        await LoadAsync(cancellationToken);
        StatusMessage = $"已固定 {entry.Name}。";
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
