using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenDbViewer.Application.Services;
using OpenDbViewer.Shell.Services;

namespace OpenDbViewer.Shell.ViewModels;

public sealed partial class HomeViewModel : ObservableObject
{
    public const string DefaultStatusMessage = "Ready.";

    private readonly DatabaseEntryService _databaseEntryService;
    private readonly IFileDialogService _fileDialogService;

    [ObservableProperty]
    private string statusMessage = DefaultStatusMessage;

    public HomeViewModel(DatabaseEntryService databaseEntryService, IFileDialogService fileDialogService)
    {
        ArgumentNullException.ThrowIfNull(databaseEntryService);
        ArgumentNullException.ThrowIfNull(fileDialogService);

        _databaseEntryService = databaseEntryService;
        _fileDialogService = fileDialogService;
    }

    [RelayCommand]
    public async Task OpenDatabaseAsync()
    {
        var filePath = _fileDialogService.PickSqliteFile();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var result = await _databaseEntryService.OpenAsync(filePath);
        StatusMessage = result.Message;
    }
}
