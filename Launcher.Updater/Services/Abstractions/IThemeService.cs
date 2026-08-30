namespace Launcher.Updater.Services.Abstractions;

public enum ThemeMode
{
    Dark,
    Light
}

public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    void ApplyTheme(ThemeMode theme);
    void InitializeTheme();
}
