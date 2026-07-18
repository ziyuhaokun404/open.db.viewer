namespace Sqlite.Viewer.ShellHost.Services;

public enum ThemePreference
{
    System,
    Light,
    Dark
}

public enum ThemeVariant
{
    Light,
    Dark
}

public interface IThemeService
{
    event EventHandler? ThemeChanged;

    ThemePreference CurrentPreference { get; }

    ThemeVariant EffectiveTheme { get; }

    void ApplyPreference(ThemePreference preference);
}
