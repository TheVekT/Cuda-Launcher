using System.Collections.ObjectModel;
using System.ComponentModel;
using Launcher.Core.Enums;
using Launcher.Core.Models;
using Launcher.Core.Services.IO;
using Launcher.Core.Services.System;
using Launcher.UI.WPF.Models;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.Stores;

public class SettingsStore: INotifyPropertyChanged
{
    //Services
    private readonly ThemeService _themeService;
    private readonly ISysInfoService _sysInfoService;
    private readonly ISettingsService _settingsService;
    
    //Attributes
    private string _currentThemePath;
    private LanguageModel _selectedLanguage;
    
    //General settings
    
    private bool _isKeepLauncherOpen;
    private bool _isEnableAutoUpdates;
    private bool _isEnableDiscordRichPresence;
    private double _uiScale;
    
    //Game settings
    
    private long _maxPhysicalRam;
    private int _selectedMaxRam;
    private bool _isEnableSnapshots;
    private bool _isGameFullScreen;
    private string _selectedResolution;
    private bool _isEnableAutoBackups;
    private BackupFrequency _selectedBackupFrequency;
    private int _maxBackupCount;
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
    }
    
    
    //Getters and Setters
    [SettingProperty]
    public bool IsKeepLauncherOpen
    {
        get => _isKeepLauncherOpen;
        set
        {
            if (_isKeepLauncherOpen != value)
            {
                _isKeepLauncherOpen = value;
                OnPropertyChanged(nameof(IsKeepLauncherOpen));
            }
        }
    }
    [SettingProperty]
    public bool IsEnableAutoUpdates
    {
        get => _isEnableAutoUpdates;
        set
        {
            if (_isEnableAutoUpdates != value)
            {
                _isEnableAutoUpdates = value;
                OnPropertyChanged(nameof(IsEnableAutoUpdates));
            }
        }
    }
    [SettingProperty]
    public bool IsEnableDiscordRichPresence
    {
        get => _isEnableDiscordRichPresence;
        set
        {
            if (_isEnableDiscordRichPresence != value)
            {
                _isEnableDiscordRichPresence = value;
                OnPropertyChanged(nameof(IsEnableDiscordRichPresence));
            }
        }
    }
    [SettingProperty]
    public long MaxPhysicalRam { get => _maxPhysicalRam; set => _maxPhysicalRam = value; }
    [SettingProperty]
    public int SelectedMaxRam
    {
        get => _selectedMaxRam;
        set
        {
            if (_selectedMaxRam != value)
            {
                _selectedMaxRam = value;
                OnPropertyChanged(nameof(SelectedMaxRam));
            }
        }
    }
    [SettingProperty]
    public bool IsEnableSnapshots
    {
        get => _isEnableSnapshots;
        set
        {
            if (_isEnableSnapshots != value)
            {
                _isEnableSnapshots = value;
                OnPropertyChanged(nameof(IsEnableSnapshots));
            }
        }
    }
    [SettingProperty]
     public bool IsGameFullScreen
    {
        get => _isGameFullScreen;
        set
        {
            if (_isGameFullScreen != value)
            {
                _isGameFullScreen = value;
                OnPropertyChanged(nameof(IsGameFullScreen));
            }
        }
    }
     

    [SettingProperty]
    public LanguageModel SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (value == null) return;
            
            var actualLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == value.Code) ?? value;

            if (_selectedLanguage != actualLanguage)
            {
                _selectedLanguage = actualLanguage;
                OnPropertyChanged(nameof(SelectedLanguage));
                Console.WriteLine($"Selected language: {actualLanguage.Name}, code: {actualLanguage.Code}");
                LocalizationService.Instance.LoadLanguage(actualLanguage.Code);
            }
        }
    }
    [SettingProperty]
    public string SelectedResolution
    {
        get => _selectedResolution;
        set
        {
            if (_selectedResolution != value)
            {
                _selectedResolution = value;
                OnPropertyChanged(nameof(SelectedResolution));
            }
        }
    }
    [SettingProperty]
    public bool IsEnableAutoBackups
    {
        get => _isEnableAutoBackups;
        set
        {
            if (_isEnableAutoBackups != value)
            {
                _isEnableAutoBackups = value;
                OnPropertyChanged(nameof(IsEnableAutoBackups));
            }
        }
    }
    [SettingProperty]
    public BackupFrequency SelectedBackupFrequency
    {
        get => _selectedBackupFrequency;
        set
        {
            if (_selectedBackupFrequency != value)
            {
                _selectedBackupFrequency = value;
                OnPropertyChanged(nameof(SelectedBackupFrequency));
            }
        }
    }
    [SettingProperty]
    public int MaxBackupCount
    {
        get => _maxBackupCount;
        set
        {
            if (_maxBackupCount != value)
            { 
                _maxBackupCount = value; 
                OnPropertyChanged(nameof(MaxBackupCount)); 
            } 
        }
    }
    [SettingProperty]
    public string JVMArguments
    { 
        get => _JVMArguments;
        set
        {
            if (_JVMArguments != value)
            {
                _JVMArguments = value;
                OnPropertyChanged(nameof(JVMArguments));
            }
        }
    }
    [SettingProperty]
    public string CurrentThemePath
    {
        get => _currentThemePath;
        set
        {
            if (_currentThemePath != value)
            {
                _currentThemePath = value;
                OnPropertyChanged(nameof(CurrentThemePath));
                    
                // Сразу применяем тему
                _themeService.ChangeTheme(_currentThemePath);
            }
        }
    }
    [SettingProperty]
    public double UiScale
    {
        get => _uiScale;
        set { if (Math.Abs(_uiScale - value) > 0.001) { _uiScale = value; OnPropertyChanged(nameof(UiScale)); } }
    }
    
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}