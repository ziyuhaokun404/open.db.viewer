using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDbViewer.Application.Services;
using OpenDbViewer.Domain.Models;
using OpenDbViewer.Shell.Services;
using System.Collections.ObjectModel;

namespace OpenDbViewer.Shell.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    public const string DefaultStatusMessage = "Ready.";

    private readonly DatabaseEntryService _databaseEntryService;
    private readonly IFileDialogService _fileDialogService;

    [ObservableProperty]
    private string statusMessage = DefaultStatusMessage;

    [ObservableProperty]
    private string searchText = string.Empty;

    public ObservableCollection<DatabaseEntry> RecentEntries { get; } = new();

    public ObservableCollection<DatabaseEntry> PinnedEntries { get; } = new();

    public ObservableCollection<DatabaseEntry> FilteredRecentEntries { get; } = new();

    public ObservableCollection<DatabaseEntry> FilteredPinnedEntries { get; } = new();

    public Func<string, CancellationToken, Task>? DatabaseOpenedAsync { get; set; }

    public HomeViewModel(DatabaseEntryService databaseEntryService, IFileDialogService fileDialogService)
    {
        ArgumentNullException.ThrowIfNull(databaseEntryService);
        ArgumentNullException.ThrowIfNull(fileDialogService);

        _databaseEntryService = databaseEntryService;
        _fileDialogService = fileDialogService;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var recentEntries = await _databaseEntryService.GetRecentAsync(cancellationToken);
        var pinnedEntries = await _databaseEntryService.GetPinnedAsync(cancellationToken);

        RecentEntries.Clear();
        foreach (var entry in recentEntries.OrderByDescending(item => item.LastOpenedAt))
        {
            RecentEntries.Add(entry);
        }

        PinnedEntries.Clear();
        foreach (var entry in pinnedEntries.OrderByDescending(item => item.LastOpenedAt))
        {
            PinnedEntries.Add(entry);
        }

        RefreshFilteredEntries();
    }

    [RelayCommand]
    public async Task OpenDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var filePath = _fileDialogService.PickSqliteFile();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var result = await _databaseEntryService.OpenAsync(filePath, cancellationToken);
        StatusMessage = result.Message;

        if (result.IsSuccess)
        {
            await LoadAsync(cancellationToken);
            if (DatabaseOpenedAsync is not null)
            {
                await DatabaseOpenedAsync(filePath, cancellationToken);
            }
        }
    }

    [RelayCommand]
    public async Task OpenRecentAsync(DatabaseEntry? entry, CancellationToken cancellationToken = default)
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

        var isPinned = PinnedEntries.Any(item => item.FilePath.Equals(entry.FilePath, StringComparison.OrdinalIgnoreCase));

        if (isPinned)
        {
            var pinnedEntry = PinnedEntries.First(item => item.FilePath.Equals(entry.FilePath, StringComparison.OrdinalIgnoreCase));
            await _databaseEntryService.UnpinAsync(pinnedEntry, cancellationToken);
            StatusMessage = $"Removed {entry.Name} from pinned.";
        }
        else
        {
            await _databaseEntryService.PinAsync(entry, cancellationToken);
            StatusMessage = $"Pinned {entry.Name}.";
        }

        await LoadAsync(cancellationToken);
    }

    partial void OnSearchTextChanged(string value) => RefreshFilteredEntries();

    private void RefreshFilteredEntries()
    {
        RefreshCollection(FilteredPinnedEntries, ApplyFilter(PinnedEntries));
        RefreshCollection(FilteredRecentEntries, ApplyFilter(RecentEntries));
    }

    private IEnumerable<DatabaseEntry> ApplyFilter(IEnumerable<DatabaseEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return entries;
        }

        return entries.Where(entry =>
            entry.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            entry.FilePath.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
    }

    private static void RefreshCollection(ObservableCollection<DatabaseEntry> target, IEnumerable<DatabaseEntry> source)
    {
        target.Clear();
        foreach (var entry in source)
        {
            target.Add(entry);
        }
    }
}
