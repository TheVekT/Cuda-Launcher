using Launcher.Infrastructure.Themes.Models;

namespace Launcher.Infrastructure.Themes.Abstractions;

public interface IThemeProvider
{
    List<ThemeModel> GetAllThemes();
    
    ThemeModel? GetThemeByFileName(string fileName);
    
    Task ImportThemeAsync(string themeFilePath);
    
    ThemeModel? ExtractAndProcessTheme(string themeFileNameOrZipPath);
}
