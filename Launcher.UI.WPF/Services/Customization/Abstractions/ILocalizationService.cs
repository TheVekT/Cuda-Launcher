using Launcher.Infrastructure.Localization.Models;
using Launcher.UI.WPF.Helpers.Localization;

namespace Launcher.UI.WPF.Services.Customization.Abstractions;

public interface ILocalizationService
{
    string this[string key] { get; }
    string this[LocKey key] { get; }

    List<LanguageModel> GetAvailableLanguages();
    
    string GetCodeByName(string name);
    
    void LoadLanguage(string langCode);
    
    Task ImportLocalization(string filePath);
}
