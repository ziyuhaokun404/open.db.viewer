using Microsoft.Win32;

using Open.Db.Viewer.Application.Abstractions;
using Open.Db.Viewer.Domain.Models;

using Wpf.Ui.Appearance;

namespace Open.Db.Viewer.ShellHost.Services;

public sealed class ThemeService : IThemeService, IDisposable
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private readonly IAppSettingsStore? _settingsStore;

    public ThemeService(IAppSettingsStore? settingsStore = null)
    {
        _settingsStore = settingsStore;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public event EventHandler? ThemeChanged;

    public ThemePreference CurrentPreference { get; private set; } = ThemePreference.System;

    public ThemeVariant EffectiveTheme { get; private set; } = ThemeVariant.Light;

    public void Initialize()
    {
        if (_settingsStore is not null)
        {
            // Synchronous bootstrap: settings are loaded earlier in App.OnStartup.
            CurrentPreference = ParseThemePreference(_settingsStore.Current.ThemePreference);
        }

        ApplyPreference(CurrentPreference, persist: false);
    }

    public void ApplyPreference(ThemePreference preference) => ApplyPreference(preference, persist: true);

    public void ToggleTheme()
    {
        var nextPreference = EffectiveTheme == ThemeVariant.Dark
            ? ThemePreference.Light
            : ThemePreference.Dark;

        ApplyPreference(nextPreference);
    }

    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
    }

    private void ApplyPreference(ThemePreference preference, bool persist)
    {
        CurrentPreference = preference;
        ApplyEffectiveTheme(ResolveTheme(preference));

        if (persist && _settingsStore is not null)
        {
            var settings = _settingsStore.Current;
            settings.ThemePreference = preference.ToString();
            _ = _settingsStore.SaveAsync(settings);
        }
    }

    private void OnUserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        if (CurrentPreference != ThemePreference.System)
        {
            return;
        }

        if (e.Category is not (UserPreferenceCategory.Color or UserPreferenceCategory.General or UserPreferenceCategory.VisualStyle))
        {
            return;
        }

        ApplyEffectiveTheme(ResolveTheme(CurrentPreference));
    }

    private void ApplyEffectiveTheme(ApplicationTheme applicationTheme)
    {
        EffectiveTheme = applicationTheme == ApplicationTheme.Dark
            ? ThemeVariant.Dark
            : ThemeVariant.Light;
        ApplicationThemeManager.Apply(applicationTheme);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private static ApplicationTheme ResolveTheme(ThemePreference preference) =>
        preference switch
        {
            ThemePreference.Light => ApplicationTheme.Light,
            ThemePreference.Dark => ApplicationTheme.Dark,
            _ => GetSystemTheme()
        };

    private static ThemePreference ParseThemePreference(string? value) =>
        Enum.TryParse<ThemePreference>(value, ignoreCase: true, out var preference)
            ? preference
            : ThemePreference.System;

    private static ApplicationTheme GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int appsUseLightTheme)
            {
                return appsUseLightTheme == 0 ? ApplicationTheme.Dark : ApplicationTheme.Light;
            }
        }
        catch
        {
            // Fall through to light.
        }

        return ApplicationTheme.Light;
    }
}
