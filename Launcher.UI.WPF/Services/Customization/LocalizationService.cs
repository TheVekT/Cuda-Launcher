using System.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Infrastructure.Localization.Abstractions;
using Launcher.Infrastructure.Localization.Models;
using Launcher.UI.WPF.Helpers.Localization;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Customization.Abstractions;

namespace Launcher.UI.WPF.Services.Customization;

public class LocalizationService : ILocalizationService, INotifyPropertyChanged
{
    public static LocalizationService Instance { get; internal set; } = null!;

    private const string DefaultLanguageCode = "en-US";

    private Dictionary<string, string> _translations = new ();
    private readonly Dictionary<string, string> _fallbackTranslations;
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

    public string this[LocKey key] => this[key.ToKeyString()];

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

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
