using Launcher.Core.Config.Models;

namespace Launcher.Core.UI.Abstractions;

public interface ILocalizationService
{
    string this[string key] { get; }

    List<LanguageModel> GetAvailableLanguages();
    
    string GetCodeByName(string name);
    
    void LoadLanguage(string langCode);
}
