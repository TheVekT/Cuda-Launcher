using System.Collections.ObjectModel; 
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Instances.Models;
using Launcher.Core.System.Abstractions;
using Launcher.Infrastructure.Config.Abstractions;
using Launcher.Infrastructure.Config.Models;
using Launcher.Infrastructure.Customization.Models;
using Launcher.Infrastructure.Localization.Models;
using Launcher.UI.WPF.Helpers.Collections;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services.Customization;
using Launcher.UI.WPF.Services.Customization.Abstractions;

namespace Launcher.UI.WPF.Stores;

public partial class SettingsStore: ObservableObject, 
    IRecipient<ThemeImportedMessage>, 
    IRecipient<LanguageImportedMessage>
{
    //Services
    private readonly IThemeService _themeService;


    //General settings
    
    [ObservableProperty]
    [property: SettingProperty]
    private bool _isKeepLauncherOpen;
    [ObservableProperty]
    [property: SettingProperty]
    private bool _isEnableAutoUpdates;
    [ObservableProperty]
    [property: SettingProperty]
    private bool _isEnableDiscordRichPresence;
    private double _uiScale;
    
    //Game settings
    
    [ObservableProperty]
    private long _maxPhysicalRam;
    [ObservableProperty]
    [property: SettingProperty]
    private int _selectedMaxRam;
    [ObservableProperty]
    [property: SettingProperty]
    private bool _isEnableSnapshots;
    [ObservableProperty]
    [property: SettingProperty]
    private bool _isGameFullScreen;
    [ObservableProperty]
    [property: SettingProperty]
    private string _selectedResolution;
    [ObservableProperty]
    [property: SettingProperty]
    private bool _isEnableAutoBackups;
    [ObservableProperty]
    [property: SettingProperty]
    private BackupFrequency _selectedBackupFrequency;
    [ObservableProperty]
    [property: SettingProperty]
    private int _maxBackupCount;
    [ObservableProperty]
    [property: SettingProperty]
    private string _jvmArguments;
    
    //Localization Settings
    
    private LanguageModel? _selectedLanguage;
    
    //Theme Settings
    
    [ObservableProperty]
    [property: SettingProperty]
    private string _currentThemePath;
    
    //Collections
    public ObservableRangeCollection<ThemeModel> AvailableThemes { get; set; } = [];
    public ObservableRangeCollection<LanguageModel> AvailableLanguages { get; set; } = [];
    public ObservableCollection<string> AvailableResolutions { get; set; } = ["Auto"];
    
    public IEnumerable<BackupFrequency> BackupFrequencyValues => Enum.GetValues(typeof(BackupFrequency)).Cast<BackupFrequency>();
    
    public SettingsStore(IThemeService themeService, ISysInfoService sysInfoService, ISettingsService settingsService)
    {
        _themeService = themeService;
        
        var themes = _themeService.ReloadThemes();
        AvailableThemes.ReplaceRange(themes);

        MaxPhysicalRam = sysInfoService.GetTotalRAMInMB();
        var availableRes = sysInfoService.GetPrimaryMonitorResolutions();
        AvailableResolutions.Clear();
        AvailableResolutions.Add("Auto");
        foreach (var res in availableRes){
            AvailableResolutions.Add(res);
        }
        
        var langs = LocalizationService.Instance.GetAvailableLanguages();
        AvailableLanguages.ReplaceRange(langs);
        
        SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == "en-US") ?? AvailableLanguages.FirstOrDefault()!;
        
        _isKeepLauncherOpen = false;
        _isEnableAutoUpdates = true;
        _isEnableDiscordRichPresence = true;
        _uiScale = 1.0;

        _selectedMaxRam = MaxPhysicalRam > 16000 ? 4096 : 2048;
        _isEnableSnapshots = false;
        _isGameFullScreen = false;
        _selectedResolution = AvailableResolutions.FirstOrDefault()!;
        _isEnableAutoBackups = true;
        _selectedBackupFrequency = BackupFrequency.Weekly;
        _maxBackupCount = 5;
        _jvmArguments = "";
        
        _currentThemePath = "default-dark.zip";
        
        settingsService.Initialize(this);
        
        _themeService.ChangeTheme(_currentThemePath);
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }
    
    public void Receive(ThemeImportedMessage message)
    {
        var lastThemePath = CurrentThemePath;
        var themes = _themeService.ReloadThemes();
        AvailableThemes.ReplaceRange(themes);
        CurrentThemePath = lastThemePath;
    }
    
    public void Receive(LanguageImportedMessage message)
    {
        var lastLangCode = SelectedLanguage?.Code;
        var langs = LocalizationService.Instance.GetAvailableLanguages();
        AvailableLanguages.ReplaceRange(langs);

        var lang = AvailableLanguages.FirstOrDefault(l => l.Code == lastLangCode);
        if (lang != null)
        {
            SelectedLanguage = lang;
        }
        else
        {
            SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == "en-US") ?? AvailableLanguages.FirstOrDefault()!;
        }
    }
    
    
    //Getters and Setters

    [SettingProperty]
    public LanguageModel? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            var actualLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == value?.Code) ?? AvailableLanguages.FirstOrDefault();

            if (_selectedLanguage != actualLanguage && actualLanguage != null)
            {
                _selectedLanguage = actualLanguage;
                OnPropertyChanged();
                Console.WriteLine($"Selected language: {actualLanguage.Name}, code: {actualLanguage.Code}");
                LocalizationService.Instance.LoadLanguage(actualLanguage.Code);
            }
        }
    }
    
    partial void OnCurrentThemePathChanged(string value)
    {
        _themeService.ChangeTheme(value);
    }
    
    [SettingProperty]
    public double UiScale
    {
        get => _uiScale;
        set
        {
            if (Math.Abs(_uiScale - value) >= 0.01)
            {
                _uiScale = value; 
                OnPropertyChanged();
            }
        }
    }
}