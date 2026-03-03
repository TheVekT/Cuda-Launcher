using System.Collections.Generic;
using System.ComponentModel;
using Launcher.Core.Models;

namespace Launcher.Core.Services.UI
{
    public interface ILocalizationService : INotifyPropertyChanged
    {
        string this[string key] { get; }

        List<LanguageModel> GetAvailableLanguages();
        
        string GetCodeByName(string name);
        
        void LoadLanguage(string langCode);
    }
}