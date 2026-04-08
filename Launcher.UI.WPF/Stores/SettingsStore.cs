using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Enums;
using Launcher.Core.Models;
using Launcher.Core.Services.IO;
using Launcher.Core.Services.System;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Models;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.Stores;

public partial class SettingsStore: ObservableObject, 
    IRecipient<ThemeImportedMessage>, 
    IRecipient<LanguageImportedMessage>
{
    //Services
    private readonly ThemeService _themeService;
    private readonly ISysInfoService _sysInfoService;
    private readonly ISettingsService _settingsService;
    
    //Attributes
    [ObservableProperty]
    [property: SettingProperty]
    private string _currentThemePath;
    private LanguageModel _selectedLanguage;
    
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
    private string _JVMArguments;
    
    //Collections
    public ObservableCollection<ThemeModel> AvailableThemes { get; set; } = new ObservableCollection<ThemeModel>();
    public ObservableCollection<LanguageModel> AvailableLanguages { get; set; } = new ObservableCollection<LanguageModel>();
    public ObservableCollection<string> AvailableResolutions { get; set; } = new ObservableCollection<string>();
    
    public IEnumerable<BackupFrequency> BackupFrequencyValues => Enum.GetValues(typeof(BackupFrequency)).Cast<BackupFrequency>();
    
    public SettingsStore(ThemeService themeService, ISysInfoService sysInfoService, ISettingsService settingsService)
    {
        _themeService = themeService;
        _sysInfoService = sysInfoService;
        _settingsService = settingsService;
        
        // 1. Загружаем доступные темы в список
        AvailableThemes.Clear();
        var themes = _themeService.ReloadThemes();
        foreach (var t in themes)
        {
            AvailableThemes.Add(t);
        }

        MaxPhysicalRam = _sysInfoService.GetTotalRAMInMB();
        var avaliableRes = _sysInfoService.GetPrimaryMonitorResolutions();
        AvailableResolutions.Clear();
        AvailableResolutions.Add("Auto");
        foreach (var res in avaliableRes){
            AvailableResolutions.Add(res);
        }
        
        AvailableLanguages.Clear();
        var langs = LocalizationService.Instance.GetAvailableLanguages();
        foreach (var lang in langs)
        {
            AvailableLanguages.Add(lang);
        }
        
        SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == "en-US") ?? AvailableLanguages.FirstOrDefault();
        
        _isKeepLauncherOpen = false;
        _isEnableAutoUpdates = true;
        _isEnableDiscordRichPresence = true;
        _uiScale = 1.0;

        _selectedMaxRam = MaxPhysicalRam > 16000 ? 4096 : 2048;
        _isEnableSnapshots = false;
        _isGameFullScreen = false;
        _selectedResolution = AvailableResolutions.FirstOrDefault();
        _isEnableAutoBackups = true;
        _selectedBackupFrequency = BackupFrequency.Weekly;
        _maxBackupCount = 5;
        _JVMArguments = "";
        
        _currentThemePath = "default-dark.zip";
        
        _settingsService.Initialize(this);
        
        _themeService.ChangeTheme(_currentThemePath);
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }
    
    public void Receive(ThemeImportedMessage message)
    {
        var lastThemePath = CurrentThemePath;
        AvailableThemes.Clear();
        var themes = _themeService.ReloadThemes();
        foreach (var theme in themes) AvailableThemes.Add(theme);
        CurrentThemePath = lastThemePath;
    }
    
    public void Receive(LanguageImportedMessage message)
    {
        var lastLangCode = SelectedLanguage.Code;
        AvailableLanguages.Clear(); 
        var langs = LocalizationService.Instance.GetAvailableLanguages();
        foreach (var lang in langs) AvailableLanguages.Add(lang);
        SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == lastLangCode);
    }
    
    
    //Getters and Setters

    [SettingProperty]
    public LanguageModel SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (value == null) return;
            
            var actualLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == value.Code) ?? AvailableLanguages.FirstOrDefault();

            if (_selectedLanguage != actualLanguage)
            {
                _selectedLanguage = actualLanguage;
                OnPropertyChanged(nameof(SelectedLanguage));
                Console.WriteLine($"Selected language: {actualLanguage.Name}, code: {actualLanguage.Code}");
                LocalizationService.Instance.LoadLanguage(actualLanguage.Code);
            }
        }
    }
    
    partial void OnCurrentThemePathChanged(string value)
    {
        _themeService.ChangeTheme(_currentThemePath);
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
                OnPropertyChanged(nameof(UiScale));
            }
        }
    }
}