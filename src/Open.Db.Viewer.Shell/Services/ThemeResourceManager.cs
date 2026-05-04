using Wpf.Ui.Appearance;

namespace Open.Db.Viewer.ShellHost.Services;

internal static class ThemeResourceManager
{
    private const string ThemeDictionaryFolder = "/Resources/Themes/AppTheme.";
    private static readonly Uri LightThemeUri = new(
        "/Open.Db.Viewer.Shell;component/Resources/Themes/AppTheme.Light.xaml",
        UriKind.Relative);
    private static readonly Uri DarkThemeUri = new(
        "/Open.Db.Viewer.Shell;component/Resources/Themes/AppTheme.Dark.xaml",
        UriKind.Relative);

    public static void Apply(ApplicationTheme applicationTheme)
    {
        if (System.Windows.Application.Current is null)
        {
            return;
        }

        var dictionaries = System.Windows.Application.Current.Resources.MergedDictionaries;
        var existingThemeDictionary = dictionaries.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.Contains(ThemeDictionaryFolder, StringComparison.OrdinalIgnoreCase) == true);

        var targetSource = applicationTheme == ApplicationTheme.Dark
            ? DarkThemeUri
            : LightThemeUri;

        if (existingThemeDictionary?.Source == targetSource)
        {
            return;
        }

        if (existingThemeDictionary is not null)
        {
            dictionaries.Remove(existingThemeDictionary);
        }

        dictionaries.Add(new System.Windows.ResourceDictionary { Source = targetSource });
    }
}
