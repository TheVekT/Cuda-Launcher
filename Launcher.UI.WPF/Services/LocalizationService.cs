using System.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Config.Models;
using Launcher.Infrastructure.Localization.Abstractions;
using Launcher.Infrastructure.Localization.Models;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Abstractions;

namespace Launcher.UI.WPF.Services;

public class LocalizationService : ILocalizationService, INotifyPropertyChanged
{
    public static LocalizationService Instance { get; internal set; }

    private const string DefaultLanguageCode = "en-US";

    private Dictionary<string, string> _translations = new Dictionary<string, string>();
    private Dictionary<string, string> _fallbackTranslations = new Dictionary<string, string>();
    private readonly ILocalizationProvider _localizationProvider;

    public LocalizationService Current => this;

    public LocalizationService(ILocalizationProvider localizationProvider)
    {
        _localizationProvider = localizationProvider;

        _fallbackTranslations = _localizationProvider.LoadTranslations(DefaultLanguageCode);
        LoadLanguage(DefaultLanguageCode);
    }

    public string this[string key]
    {
        get
        {
            if (_translations.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                return value;

            if (_fallbackTranslations.TryGetValue(key, out var fallbackValue) && !string.IsNullOrEmpty(fallbackValue))
                return fallbackValue;

            return key;
        }
    }

    public async Task ImportLocalization(string filePath)
    {
        await _localizationProvider.ImportLanguageAsync(filePath);
        WeakReferenceMessenger.Default.Send(new LanguageImportedMessage());
    }

    public List<LanguageModel> GetAvailableLanguages()
    {
        return _localizationProvider.GetAvailableLanguages();
    }

    public string GetCodeByName(string name)
    {
        return _localizationProvider.GetCodeByName(name);
    }

    public void LoadLanguage(string langCode)
    {
        _translations = _localizationProvider.LoadTranslations(langCode);
        OnPropertyChanged("Item[]");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
