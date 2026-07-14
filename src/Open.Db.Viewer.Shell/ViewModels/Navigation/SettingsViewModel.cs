using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Domain.Models;
using Open.Db.Viewer.ShellHost.Services;

namespace Open.Db.Viewer.Shell.ViewModels.Navigation;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly IAppSettingsStore _settingsStore;

    [ObservableProperty]
    private int queryMaxResultRows = AppSettings.DefaultQueryMaxResultRows;

    [ObservableProperty]
    private int queryTimeoutSeconds = AppSettings.DefaultQueryTimeoutSeconds;

    [ObservableProperty]
    private int defaultPageSize = AppSettings.DefaultPageSize;

    [ObservableProperty]
    private string statusMessage = "设置会自动保存到本机。";

    public SettingsViewModel(IThemeService themeService, IAppSettingsStore settingsStore)
    {
        ArgumentNullException.ThrowIfNull(themeService);
        ArgumentNullException.ThrowIfNull(settingsStore);

        _themeService = themeService;
        _settingsStore = settingsStore;
        _themeService.ThemeChanged += OnThemeChanged;

        var settings = _settingsStore.Current;
        QueryMaxResultRows = settings.QueryMaxResultRows;
        QueryTimeoutSeconds = settings.QueryTimeoutSeconds;
        DefaultPageSize = settings.DefaultPageSizeValue;
    }

    public string Title => "设置";

    public IReadOnlyList<int> PageSizeOptions { get; } = [50, 100, 200, 500];

    public IReadOnlyList<int> QueryMaxResultRowsOptions { get; } = [1_000, 5_000, 10_000, 20_000, 50_000];

    public IReadOnlyList<int> QueryTimeoutSecondsOptions { get; } = [0, 15, 30, 60, 120, 300];

    public string ThemePreferenceLabel => _themeService.CurrentPreference switch
    {
        ThemePreference.System => "跟随系统",
        ThemePreference.Light => "浅色模式",
        ThemePreference.Dark => "深色模式",
        _ => "跟随系统"
    };

    public string EffectiveThemeLabel => _themeService.EffectiveTheme == ThemeVariant.Dark
        ? "深色"
        : "浅色";

    public string ThemeSummary => $"当前设置：{ThemePreferenceLabel}，实际显示：{EffectiveThemeLabel}";

    public string QueryTimeoutLabel => QueryTimeoutSeconds == 0
        ? "不超时（仍可手动取消）"
        : $"{QueryTimeoutSeconds} 秒";

    [RelayCommand]
    public void SetThemeSystem() => ApplyTheme(ThemePreference.System);

    [RelayCommand]
    public void SetThemeLight() => ApplyTheme(ThemePreference.Light);

    [RelayCommand]
    public void SetThemeDark() => ApplyTheme(ThemePreference.Dark);

    partial void OnQueryMaxResultRowsChanged(int value) => _ = PersistSettingsAsync();

    partial void OnQueryTimeoutSecondsChanged(int value)
    {
        OnPropertyChanged(nameof(QueryTimeoutLabel));
        _ = PersistSettingsAsync();
    }

    partial void OnDefaultPageSizeChanged(int value) => _ = PersistSettingsAsync();

    private void ApplyTheme(ThemePreference preference)
    {
        _themeService.ApplyPreference(preference);
        StatusMessage = $"主题已切换为：{ThemePreferenceLabel}。";
    }

    private async Task PersistSettingsAsync()
    {
        var settings = _settingsStore.Current;
        settings.QueryMaxResultRows = QueryMaxResultRows;
        settings.QueryTimeoutSeconds = QueryTimeoutSeconds;
        settings.DefaultPageSizeValue = DefaultPageSize;
        settings.ThemePreference = _themeService.CurrentPreference.ToString();
        await _settingsStore.SaveAsync(settings);
        StatusMessage = "设置已保存。";
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(ThemePreferenceLabel));
        OnPropertyChanged(nameof(EffectiveThemeLabel));
        OnPropertyChanged(nameof(ThemeSummary));
    }
}
