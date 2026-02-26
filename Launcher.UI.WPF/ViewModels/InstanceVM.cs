using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using Launcher.Core.Models;
using Launcher.Core.Services.Game;
using Launcher.Core.Services.IO;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels;

public class InstanceVM: INotifyPropertyChanged
{
    //Services
    private readonly IGameVersionService _versionService;
    private readonly IInstanceService _instanceService;
    private readonly IInstanceFileSystemService _instanceFileSystemService;
    
    //Stores
    private readonly InstancesStore _instancesStore;
    private readonly SettingsStore _settingsStore;
    
    public bool CreatingPage1Visible { get; set; } = false;
    public bool CreatingPage2Visible { get; set; } = true;
    
    private string _selectedIcon;
    private string _selectedGameVersion;
    private string _installationName;
    private string _selectedModLoader;
    private string _selectedLoaderVersion;
    private bool _useGlobalGameSettings;
    private bool _useGlobalBackupSettings;
    private bool _isCreatingInstance;
    private IsolationType _selectedIsolation = IsolationType.Global;
    
    private int _selectedMaxRam;
    private bool _isGameFullscreen;
    private string _selectedResolution;
    private bool _isEnableAutoBackups;
    private BackupFrequency _selectedBackupFrequency;
    private int _maxBackupCount;
    private string _JVMArguments;
    
    //Commands
    public ICommand ToggleCreatingPageCommand { get; }
    public ICommand CloseSelfCommand { get; }
    public ICommand CreateInstanceCommand => new RelayCommand(async o => await CreateInstance());
    
    //Events
    public event Action RequestClose;
    
    //Collections
    public ObservableCollection<string> GameVersions { get; } = new(); 
    public ObservableCollection<string> LoaderVersions { get; } = new();
    
    //public properties
    public InstancesStore InstancesStore => _instancesStore;
    public SettingsStore SettingsStore => _settingsStore;
    
    public InstanceVM(IGameVersionService versionService, 
        IInstanceService instanceService, 
        IInstanceFileSystemService instanceFileSystemService,
        InstancesStore instancesStore, 
        SettingsStore settingsStore)
    {
        _versionService = versionService;
        _instanceService = instanceService;
        _instanceFileSystemService = instanceFileSystemService;
        
        _instancesStore = instancesStore;
        _settingsStore = settingsStore;
        
        InstallationName = string.Empty;
        SelectedIsolation = IsolationType.Global;
        
        SelectedMaxRam = SettingsStore.SelectedMaxRam;
        IsGameFullscreen = SettingsStore.IsGameFullScreen;
        SelectedResolution = SettingsStore.SelectedResolution;
        IsEnableAutoBackups = SettingsStore.IsEnableAutoBackups;
        SelectedBackupFrequency = SettingsStore.SelectedBackupFrequency;
        MaxBackupCount = SettingsStore.MaxBackupCount;
        JVMArguments = SettingsStore.JVMArguments;

        UseGlobalGameSettings = true;
        UseGlobalBackupSettings = true;
        
        
        bool typeChanged = _selectedModLoader != "Vanilla";
        _selectedModLoader = "Vanilla"; 
        
        OnPropertyChanged(nameof(SelectedModLoader)); 
        
        CreatingPage1Visible = true;
        CreatingPage2Visible = false;
        
        if (_instancesStore.IconList.Count > 0){
            SelectedIcon = _instancesStore.IconList.FirstOrDefault();;
        }
        ToggleCreatingPageCommand = new RelayCommand(o =>
        {
            CreatingPage1Visible = !CreatingPage1Visible;
            CreatingPage2Visible = !CreatingPage2Visible;
            OnPropertyChanged(nameof(CreatingPage1Visible));
            OnPropertyChanged(nameof(CreatingPage2Visible));
        });
        CloseSelfCommand = new RelayCommand(o => RequestClose?.Invoke());
        
    }
    public async Task InitializeAsync()
    {
        await RefreshGameVersions();
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
            LoaderVersions.Clear();
            System.Windows.Application.Current.Dispatcher.Invoke(() => SelectedLoaderVersion = null);

            var type = GetLoaderType(_selectedModLoader);

            // Если это Vanilla или версия игры еще не выбрана — очищаем список и выходим
            if (type == GameLoaderType.Vanilla || string.IsNullOrEmpty(SelectedGameVersion))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => LoaderVersions.Clear());
                return;
            }

            // Запрашиваем все версии лоадера для выбранной версии игры
            var loadedVersions = await _versionService.GetLoaderVersionsAsync(type, SelectedGameVersion);
            var versionList = loadedVersions.ToList();

            // Запрашиваем рекомендуемую (стабильную) версию
            var recommendedVersion = await _versionService.GetRecommendedLoaderVersionAsync(type, SelectedGameVersion);

            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                LoaderVersions.Clear();
                foreach (var version in versionList) 
                {
                    LoaderVersions.Add(version);
                }
            });

            await Task.Delay(50); // Небольшая задержка для UI

            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                if (LoaderVersions.Count > 0)
                {
                    // Ставим стабильную версию по умолчанию. Если ее нет - первую в списке.
                    SelectedLoaderVersion = recommendedVersion ?? LoaderVersions.FirstOrDefault();
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VM] Ошибка RefreshLoaderVersions: {ex.Message}");
        }
    }
    
    private async Task RefreshGameVersions()
    {
        try 
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => SelectedGameVersion = null);

            GameLoaderType type = GetLoaderType(_selectedModLoader);
            
            var loadedVersions = await _versionService.GetGameVersionsByTypeAsync(type);
            var versionList = loadedVersions.ToList(); 

            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                GameVersions.Clear();
                if (versionList.Count == 0) return;
                foreach (var version in versionList) GameVersions.Add(version);
            });
            
            await Task.Delay(50);

            System.Windows.Application.Current.Dispatcher.Invoke(() => 
            {
                if (GameVersions.Count > 0) SelectedGameVersion = GameVersions[0];
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VM] Ошибка RefreshGameVersions: {ex.Message}");
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
    
    private async Task CreateInstance()
    {
        if (string.IsNullOrEmpty(SelectedGameVersion)) return;
        IsCreatingInstance = true;
        var finalName = string.IsNullOrWhiteSpace(InstallationName) 
            ? SuggestedName 
            : InstallationName;

        var newInstance = new MinecraftInstance
        {
            Id = Guid.NewGuid().ToString(), // Обязательно генерируем ID тут
            Name = finalName,
            IconPath = !string.IsNullOrEmpty(SelectedIcon) 
                ? Path.GetFileName(SelectedIcon) 
                : Path.GetFileName(_instancesStore.IconList.FirstOrDefault()),
            GameVersion = SelectedGameVersion,
            LoaderVersion = (SelectedModLoader == "Vanilla") ? null : SelectedLoaderVersion,
            LoaderType = GetLoaderType(SelectedModLoader),
            IsolationType = SelectedIsolation,
            LastPlayedDate = null,
            GameSettings = new GameSettings
            {
                AllocatedMemory = UseGlobalGameSettings ? null : (int?)SelectedMaxRam,
                Fullscreen = UseGlobalGameSettings ? null : (bool?)IsGameFullscreen,
                GameResolution = UseGlobalGameSettings ? null : SelectedResolution,
                JvmArgs = UseGlobalGameSettings ? null : JVMArguments
            },
            BackupSettings = new BackupSettings
            {
                SavesBackupSettings = UseGlobalBackupSettings 
                    ? BackupPolicy.Inherit 
                    : (IsEnableAutoBackups ? BackupPolicy.ForceOn : BackupPolicy.ForceOff),
                SavesBackupFrequency = UseGlobalBackupSettings ? null : (BackupFrequency?)SelectedBackupFrequency,
                SavesMaxBackups = UseGlobalBackupSettings ? null : (int?)MaxBackupCount,
                LastBackupDate = null
            }
        };

        try
        {
            await _instanceFileSystemService.InitializeOnCreation(newInstance);
            _instancesStore.Instances.Add(newInstance);
            _instanceService.SaveInstances(_instancesStore.Instances);

            RequestClose?.Invoke();
            Debug.WriteLine($"Created instance: {finalName}");
            _instancesStore.InvokeAddedInstance();
            _instancesStore.SelectedInstance = newInstance;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Ошибка создания инстанса: {ex.Message}", "Ошибка",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsCreatingInstance = false;
        }
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
    
    public bool IsCreatingInstance
    {
        get => _isCreatingInstance;
        set
        {
            if (_isCreatingInstance != value)
            {
                _isCreatingInstance = value;
                OnPropertyChanged(nameof(IsCreatingInstance));
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
                
                _ = RefreshLoaderVersions(); 
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
    public IsolationType SelectedIsolation
    {
        get => _selectedIsolation;
        set
        {
            if (_selectedIsolation != value)
            {
                _selectedIsolation = value;
                OnPropertyChanged(nameof(SelectedIsolation));
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
                if (_selectedModLoader == "Vanilla")
                {
                    SelectedIsolation = IsolationType.Global;
                }
                else
                {
                    SelectedIsolation = IsolationType.Full;
                }
                OnPropertyChanged(nameof(SelectedModLoader));
                _ = RefreshGameVersions();
                _ = RefreshLoaderVersions();
                OnPropertyChanged(nameof(SuggestedName));
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}