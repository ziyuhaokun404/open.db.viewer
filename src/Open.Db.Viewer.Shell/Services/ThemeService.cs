using Microsoft.Win32;

using Wpf.Ui.Appearance;

namespace Open.Db.Viewer.ShellHost.Services;

public enum ThemePreference
{
    System,
    Light,
    Dark
}

public sealed class ThemeService : IDisposable
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public ThemeService()
    {
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public event EventHandler? ThemeChanged;

    public ThemePreference CurrentPreference { get; private set; } = ThemePreference.System;

    public ApplicationTheme EffectiveTheme { get; private set; } = ApplicationTheme.Light;

    public void Initialize() => ApplyPreference(CurrentPreference);

    public void ApplyPreference(ThemePreference preference)
    {
        CurrentPreference = preference;
        ApplyEffectiveTheme(ResolveTheme(preference));
    }

    public void ToggleTheme()
    {
        var nextPreference = EffectiveTheme == ApplicationTheme.Dark
            ? ThemePreference.Light
            : ThemePreference.Dark;

        ApplyPreference(nextPreference);
    }

    public void Dispose()
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
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
        EffectiveTheme = applicationTheme;
        ApplicationThemeManager.Apply(applicationTheme);
        ThemeResourceManager.Apply(applicationTheme);
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    private static ApplicationTheme ResolveTheme(ThemePreference preference) =>
        preference switch
        {
            ThemePreference.Light => ApplicationTheme.Light,
            ThemePreference.Dark => ApplicationTheme.Dark,
            _ => GetSystemTheme()
        };

    private static ApplicationTheme GetSystemTheme()
    {
        try
        {
            using var personalizeKey = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath);
            var appsUseLightTheme = personalizeKey?.GetValue("AppsUseLightTheme");

            return appsUseLightTheme switch
            {
                0 => ApplicationTheme.Dark,
                int lightThemeFlag when lightThemeFlag > 0 => ApplicationTheme.Light,
                _ => ApplicationTheme.Light
            };
        }
        catch
        {
            return ApplicationTheme.Light;
        }
    }
}
