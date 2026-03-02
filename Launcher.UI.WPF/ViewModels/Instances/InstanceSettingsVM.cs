using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Launcher.Core.Models;
using Launcher.Core.Services.Game;
using Launcher.Core.Services.IO;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels;

public class InstanceSettingsVM : INotifyPropertyChanged
{
        //Services
    private readonly IGameVersionService _versionService;
    private readonly IInstanceService _instanceService;
    private readonly IInstanceFileSystemService _instanceFileSystemService;
    
    //Stores
    private readonly InstancesStore _instancesStore;
    private readonly SettingsStore _settingsStore;
    
    //Attributes
    private MinecraftInstance _instance;
    
    private string _selectedIcon;
    private string _selectedGameVersion;
    private string _installationName;
    private string _selectedModLoader;
    private string _selectedLoaderVersion;
    private bool _useGlobalGameSettings;
    private bool _useGlobalBackupSettings;
    
    private int _selectedMaxRam;
    private bool _isGameFullscreen;
    private string _selectedResolution;
    private bool _isEnableAutoBackups;
    private BackupFrequency _selectedBackupFrequency;
    private int _maxBackupCount;
    private string _JVMArguments;
    
    //Commands
    public ICommand CloseSelfCommand { get; }
    public ICommand SaveCommand { get; }

    //Events
    public event Action RequestClose;
    
    //Collections
    public ObservableCollection<string> LoaderVersions { get; } = new();
    
    //public properties
    public InstancesStore InstancesStore => _instancesStore;
    public SettingsStore SettingsStore => _settingsStore;
    
    public InstanceSettingsVM(MinecraftInstance instance,
        IGameVersionService versionService, 
        IInstanceService instanceService, 
        IInstanceFileSystemService instanceFileSystemService,
        InstancesStore instancesStore, 
        SettingsStore settingsStore)
    {
        _instance = instance;
        
        _versionService = versionService;
        _instanceService = instanceService;
        _instanceFileSystemService = instanceFileSystemService;
        
        _instancesStore = instancesStore;
        _settingsStore = settingsStore;
        
        InstallationName = _instance.Name;
        SelectedGameVersion = _instance.GameVersion;
        SelectedLoaderVersion = _instance.LoaderType == GameLoaderType.Vanilla ? null : _instance.LoaderVersion;
        SelectedIcon = _instancesStore.IconList.FirstOrDefault(fullPath => Path.GetFileName(fullPath) == _instance.IconPath);
        UseGlobalGameSettings = _instance.GameSettings.AllocatedMemory == null;
        if (UseGlobalGameSettings)
        {
            RefreshGlobalGameSettings();
        }
        else
        {
            SelectedMaxRam = _instance.GameSettings.AllocatedMemory.Value;
            IsGameFullscreen = _instance.GameSettings.Fullscreen.Value;
            SelectedResolution = _instance.GameSettings.GameResolution;
            JVMArguments = _instance.GameSettings.JvmArgs;
        }
        UseGlobalBackupSettings = _instance.BackupSettings.SavesBackupSettings == BackupPolicy.Inherit;
        if (UseGlobalBackupSettings)
        {
            RefreshGlobalBackupSettings();
        }
        else
        {
            IsEnableAutoBackups = _instance.BackupSettings.SavesBackupSettings == BackupPolicy.ForceOn;
            SelectedBackupFrequency = _instance.BackupSettings.SavesBackupFrequency.Value;
            MaxBackupCount = _instance.BackupSettings.SavesMaxBackups.Value;
        }
        
        _selectedModLoader = _instance.LoaderType.ToString();; 
        
        OnPropertyChanged(nameof(SelectedModLoader)); 
        
        
        CloseSelfCommand = new RelayCommand(o => RequestClose?.Invoke());
        SaveCommand = new RelayCommand(o => SaveNewInstanceSettings());
    }
    public async Task InitializeAsync()
    {
        await RefreshLoaderVersions();
    }
    
    
    private void SaveNewInstanceSettings()
    {
        var finalName = string.IsNullOrWhiteSpace(InstallationName) 
            ? SuggestedName 
            : InstallationName;

        _instance.Name = finalName;
        _instance.IconPath = !string.IsNullOrEmpty(SelectedIcon) 
            ? Path.GetFileName(SelectedIcon) 
            : Path.GetFileName(_instancesStore.IconList.FirstOrDefault());
        _instance.LoaderVersion = (SelectedModLoader == "Vanilla") ? null : SelectedLoaderVersion;
        _instance.GameSettings.AllocatedMemory = UseGlobalGameSettings ? null : (int?)SelectedMaxRam;
        _instance.GameSettings.Fullscreen = UseGlobalGameSettings ? null : (bool?)IsGameFullscreen;
        _instance.GameSettings.GameResolution =
            UseGlobalGameSettings ? null : (IsGameFullscreen ? "Auto" : SelectedResolution);
        _instance.GameSettings.JvmArgs = UseGlobalGameSettings ? null : JVMArguments;
        _instance.BackupSettings.SavesBackupSettings = UseGlobalBackupSettings
            ? BackupPolicy.Inherit
            : (IsEnableAutoBackups ? BackupPolicy.ForceOn : BackupPolicy.ForceOff);
        _instance.BackupSettings.SavesBackupFrequency =
            UseGlobalBackupSettings ? null : (BackupFrequency?)SelectedBackupFrequency;
        _instance.BackupSettings.SavesMaxBackups = UseGlobalBackupSettings ? null : (int?)MaxBackupCount;
        
        try
        {
            _instanceService.SaveInstances(_instancesStore.Instances);

            RequestClose?.Invoke();
            Debug.WriteLine($"Modified Instance: {finalName}");
            _instancesStore.InvokeAddedInstance();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error saving instance settings: {ex.Message}", "Ошибка",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void RefreshGlobalGameSettings()
    {
        SelectedMaxRam = SettingsStore.SelectedMaxRam;
        IsGameFullscreen = SettingsStore.IsGameFullScreen;
        SelectedResolution = SettingsStore.SelectedResolution;
        JVMArguments = SettingsStore.JVMArguments;
    }
    
    private void RefreshGlobalBackupSettings()
    {
        IsEnableAutoBackups = SettingsStore.IsEnableAutoBackups;
        SelectedBackupFrequency = SettingsStore.SelectedBackupFrequency;
        MaxBackupCount = SettingsStore.MaxBackupCount;
    }

    private async Task RefreshLoaderVersions()
    {
        try
        {
            var type = GetLoaderType(_selectedModLoader);

            // Если это Vanilla — список пуст
            if (type == GameLoaderType.Vanilla || string.IsNullOrEmpty(SelectedGameVersion))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => LoaderVersions.Clear());
                return;
            }

            // Запрашиваем все версии лоадера для заблокированной версии игры
            var loadedVersions = await _versionService.GetLoaderVersionsAsync(type, SelectedGameVersion);
            var versionList = loadedVersions.ToList();

            // 1. Запоминаем версию, которая уже установлена в инстансе (мы передали её в конструкторе)
            string currentSavedVersion = _selectedLoaderVersion;

            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                LoaderVersions.Clear();
                foreach (var version in versionList) 
                {
                    LoaderVersions.Add(version);
                }

                // 2. Просто возвращаем текущую версию на место, чтобы ComboBox показал её
                if (!string.IsNullOrEmpty(currentSavedVersion) && LoaderVersions.Contains(currentSavedVersion))
                {
                    SelectedLoaderVersion = currentSavedVersion;
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VM] Ошибка RefreshLoaderVersions: {ex.Message}");
        }
    }
    
    private GameLoaderType GetLoaderType(string uiName)
    {
        return uiName switch
        {
            "Forge" => GameLoaderType.Forge,
            "NeoForge" => GameLoaderType.NeoForge,
            "Fabric" => GameLoaderType.Fabric,
            "Quilt" => GameLoaderType.Quilt,
            _ => GameLoaderType.Vanilla
        };
    }
    
    
    //Getters and Setters
    
    
    public bool UseGlobalGameSettings
    {
        get => _useGlobalGameSettings;
        set
        {
            if (_useGlobalGameSettings != value)
            {
                if (value) RefreshGlobalGameSettings();
                _useGlobalGameSettings = value;
                OnPropertyChanged(nameof(UseGlobalGameSettings));
            }
        }
    }
    public bool UseGlobalBackupSettings
    {
        get => _useGlobalBackupSettings;
        set
        {
            if (_useGlobalBackupSettings != value) {
                if (value) RefreshGlobalBackupSettings();
                _useGlobalBackupSettings = value;
                OnPropertyChanged(nameof(UseGlobalBackupSettings));
            }
        }
    }
    
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
    
    public bool IsGameFullscreen
    {
        get => _isGameFullscreen;
        set
        {
            if (_isGameFullscreen != value)
            {
                _isGameFullscreen = value;
                OnPropertyChanged(nameof(IsGameFullscreen));
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
    

    public string SelectedLoaderVersion
    {
        get => _selectedLoaderVersion;
        set
        {
            if (_selectedLoaderVersion != value)
            {
                _selectedLoaderVersion = value;
                OnPropertyChanged(nameof(SelectedLoaderVersion));
            }
        }
    }
    
    public string SelectedIcon
    {
        get => _selectedIcon;
        set
        {
            if (_selectedIcon != value)
            {
                _selectedIcon = value;
                OnPropertyChanged(nameof(SelectedIcon));
            }
        }
    }
    public string SelectedGameVersion
    {
        get => _selectedGameVersion;
        set
        {
            if (_selectedGameVersion != value)
            {
                _selectedGameVersion = value;
                OnPropertyChanged(nameof(SelectedGameVersion));
                OnPropertyChanged(nameof(SuggestedName));
            }
        }
    }
    public string InstallationName
    {
        get => _installationName;
        set
        {
            if (_installationName != value)
            {
                _installationName = value;
                OnPropertyChanged(nameof(InstallationName));
            }
        }
    }
    public string SuggestedName
    {
        get
        {
            if (string.IsNullOrEmpty(SelectedGameVersion)) return "New Installation";
            return $"{SelectedModLoader} {SelectedGameVersion}";
        }
    }
    public string SelectedModLoader
    {
        get => _selectedModLoader;
        set
        {
            if (_selectedModLoader != value)
            {
                _selectedModLoader = value;
                OnPropertyChanged(nameof(SelectedModLoader));
                OnPropertyChanged(nameof(SuggestedName));
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}