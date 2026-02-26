using System.Collections.ObjectModel;
using System.ComponentModel;
using Launcher.Core.Models;
using Launcher.Core.Services.System;
using Launcher.UI.WPF.Models;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.Stores;

public class SettingsStore: INotifyPropertyChanged
{
    //Services
    private readonly ThemeService _themeService;
    private readonly ISysInfoService _sysInfoService;
    
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
    
    public SettingsStore(ThemeService themeService, ISysInfoService sysInfoService)
    {
        _themeService = themeService;
        _sysInfoService = sysInfoService;

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
        
        IsKeepLauncherOpen = false;
        IsEnableAutoUpdates = true;
        IsEnableDiscordRichPresence = true;
        UiScale = 1.0;

        SelectedMaxRam = MaxPhysicalRam > 16000 ? 4096 : 2048;
        IsEnableSnapshots = false;
        IsGameFullScreen = false;
        SelectedResolution = AvailableResolutions.FirstOrDefault();
        IsEnableAutoBackups = true;
        SelectedBackupFrequency = BackupFrequency.Weekly;
        MaxBackupCount = 5;
        JVMArguments = "";
        
        _currentThemePath = "";
        //Load settings from storage (not implemented yet)
    }
    
    
    //Getters and Setters
    
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
    
    public long MaxPhysicalRam { get => _maxPhysicalRam; set => _maxPhysicalRam = value; }
    
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
     

    
    public LanguageModel SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (_selectedLanguage != value && value != null)
            {
                _selectedLanguage = value;
                OnPropertyChanged(nameof(SelectedLanguage));
                Console.WriteLine($"Selected language: {value.Name}, code: {value.Code}");
                LocalizationService.Instance.LoadLanguage(value.Code);
            }
        }
    }
    
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
    public double UiScale
    {
        get => _uiScale;
        set { if (Math.Abs(_uiScale - value) > 0.001) { _uiScale = value; OnPropertyChanged(nameof(UiScale)); } }
    }
    
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}