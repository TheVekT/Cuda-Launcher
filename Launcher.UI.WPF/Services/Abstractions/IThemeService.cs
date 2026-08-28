using Launcher.Infrastructure.Customization.Models;

namespace Launcher.UI.WPF.Services.Abstractions;

public interface IThemeService
{
    ThemeModel? CurrentTheme { get; }

    ThemeModel? GetThemeByFileName(string fileName);

    Task ImportTheme(string themeFilePath);

    List<ThemeModel> ReloadThemes();

    void ChangeTheme(string themeFileName);
}