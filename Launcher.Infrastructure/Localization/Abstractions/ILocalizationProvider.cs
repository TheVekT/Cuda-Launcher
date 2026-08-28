using Launcher.Infrastructure.Localization.Models;

namespace Launcher.Infrastructure.Localization.Abstractions;

public interface ILocalizationProvider
{
    List<LanguageModel> GetAvailableLanguages();
    
    string GetCodeByName(string name);
    
    Dictionary<string, string> LoadTranslations(string langCode);
    
    Task ImportLanguageAsync(string filePath);
}
