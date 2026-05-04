using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Open.Db.Viewer.ShellHost.Services;

namespace Open.Db.Viewer.Shell.ViewModels.Navigation;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ThemeService _themeService;

    public SettingsViewModel(ThemeService? themeService = null)
    {
        _themeService = themeService ?? new ThemeService();
        _themeService.ThemeChanged += OnThemeChanged;
    }

    public string Title => "设置";

    public string ThemePreferenceLabel => _themeService.CurrentPreference switch
    {
        ThemePreference.System => "跟随系统",
        ThemePreference.Light => "浅色模式",
        ThemePreference.Dark => "深色模式",
        _ => "跟随系统"
    };

    public string EffectiveThemeLabel => _themeService.EffectiveTheme == Wpf.Ui.Appearance.ApplicationTheme.Dark
        ? "深色"
        : "浅色";

    public string ThemeSummary => $"当前设置：{ThemePreferenceLabel}，实际显示：{EffectiveThemeLabel}";

    [RelayCommand]
    private void UseSystemTheme() => _themeService.ApplyPreference(ThemePreference.System);

    [RelayCommand]
    private void UseLightTheme() => _themeService.ApplyPreference(ThemePreference.Light);

    [RelayCommand]
    private void UseDarkTheme() => _themeService.ApplyPreference(ThemePreference.Dark);

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(ThemePreferenceLabel));
        OnPropertyChanged(nameof(EffectiveThemeLabel));
        OnPropertyChanged(nameof(ThemeSummary));
    }
}
