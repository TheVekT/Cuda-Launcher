using Launcher.Infrastructure.Customization.Models;

namespace Launcher.Infrastructure.Customization.Abstractions;

public interface IThemeProvider
{
    List<ThemeModel> GetAllThemes();
    
    ThemeModel? GetThemeByFileName(string fileName);
    
    Task ImportThemeAsync(string themeFilePath);
    
    ThemeModel? ExtractAndProcessTheme(string themeFileNameOrZipPath);
}
