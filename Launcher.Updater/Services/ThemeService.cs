using System.Diagnostics;
using System.Windows;
using Launcher.Updater.Services.Abstractions;
using Microsoft.Win32;

namespace Launcher.Updater.Services;

public class ThemeService : IThemeService
{
    private const string DarkThemeUri = "pack://application:,,,/Resources/Themes/Theme.Dark.xaml";
    private const string LightThemeUri = "pack://application:,,,/Resources/Themes/Theme.Light.xaml";

    public ThemeMode CurrentTheme { get; private set; } = ThemeMode.Dark;

    public void InitializeTheme()
    {
        bool isDark = IsSystemDarkTheme();
        ApplyTheme(isDark ? ThemeMode.Dark : ThemeMode.Light);
    }

    public void ApplyTheme(ThemeMode theme)
    {
        CurrentTheme = theme;
        string targetUri = theme == ThemeMode.Light ? LightThemeUri : DarkThemeUri;

        try
        {
            var newDict = new ResourceDictionary { Source = new Uri(targetUri, UriKind.Absolute) };
            var merged = Application.Current.Resources.MergedDictionaries;

            for (int i = 0; i < merged.Count; i++)
            {
                var source = merged[i].Source?.OriginalString;
                if (source != null && (source.Contains("Theme.Dark.xaml") || source.Contains("Theme.Light.xaml")))
                {
                    merged[i] = newDict;
                    return;
                }
            }

            merged.Insert(0, newDict);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ThemeService] Failed to apply theme: {ex.Message}");
        }
    }

    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            if (value is int intValue)
            {
                return intValue == 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ThemeService] Failed to read Windows theme from registry: {ex.Message}");
        }

        return true; // Default to dark
    }
}
