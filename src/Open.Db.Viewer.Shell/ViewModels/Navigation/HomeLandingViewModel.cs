using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Open.Db.Viewer.Application.Services;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.Shell.Services;
using System.Collections.ObjectModel;

namespace Open.Db.Viewer.Shell.ViewModels.Navigation;

public sealed partial class HomeLandingViewModel : ObservableObject
{
    private readonly DatabaseEntryService _databaseEntryService;
    private readonly IFileDialogService _fileDialogService;

    public HomeLandingViewModel(DatabaseEntryService databaseEntryService, IFileDialogService fileDialogService)
    {
        ArgumentNullException.ThrowIfNull(databaseEntryService);
        ArgumentNullException.ThrowIfNull(fileDialogService);

        _databaseEntryService = databaseEntryService;
        _fileDialogService = fileDialogService;
    }

    [ObservableProperty]
    private DatabaseEntry? quickOpenEntry;

    [ObservableProperty]
    private string statusMessage = StatusMessages.DefaultReady;

    public ObservableCollection<DatabaseEntry> RecentSummary { get; } = new();

    public ObservableCollection<DatabaseEntry> PinnedSummary { get; } = new();

    public bool HasPinnedEntries => PinnedSummary.Count > 0;

    public bool HasRecentEntries => RecentSummary.Count > 0;

    public Func<string, CancellationToken, Task>? DatabaseOpenedAsync { get; set; }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var recentEntries = await _databaseEntryService.GetRecentAsync(cancellationToken);
        var pinnedEntries = await _databaseEntryService.GetPinnedAsync(cancellationToken);

        QuickOpenEntry = recentEntries
            .OrderByDescending(item => item.LastOpenedAt)
            .FirstOrDefault();

        Refresh(RecentSummary, recentEntries
            .OrderByDescending(item => item.LastOpenedAt)
            .Take(3));

        Refresh(PinnedSummary, pinnedEntries
            .OrderByDescending(item => item.LastOpenedAt)
            .Take(3));
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
    public async Task OpenQuickOpenAsync(CancellationToken cancellationToken = default)
    {
        if (QuickOpenEntry is null)
        {
            return;
        }

        var result = await _databaseEntryService.OpenAsync(QuickOpenEntry.FilePath, cancellationToken);
        StatusMessage = result.Message;

        if (result.IsSuccess)
        {
            await LoadAsync(cancellationToken);
            if (DatabaseOpenedAsync is not null)
            {
                await DatabaseOpenedAsync(QuickOpenEntry.FilePath, cancellationToken);
            }
        }
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

        if (entry.IsPinned)
        {
            await _databaseEntryService.UnpinAsync(entry, cancellationToken);
        }
        else
        {
            await _databaseEntryService.PinAsync(entry, cancellationToken);
        }

        await LoadAsync(cancellationToken);
    }

    private void Refresh(ObservableCollection<DatabaseEntry> target, IEnumerable<DatabaseEntry> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }

        OnPropertyChanged(nameof(HasPinnedEntries));
        OnPropertyChanged(nameof(HasRecentEntries));
    }
}
