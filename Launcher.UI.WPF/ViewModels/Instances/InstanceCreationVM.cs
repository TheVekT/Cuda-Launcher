using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Enums;
using Launcher.Core.Messages;
using Launcher.Core.Models;
using Launcher.Core.Services.Game;
using Launcher.Core.Services.IO;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels.Instances;

public partial class InstanceCreationVM: ObservableObject
{
    //Services
    private readonly IGameVersionService _versionService;
    
    //Stores
    private readonly InstancesStore _instancesStore;
    private readonly SettingsStore _settingsStore;
    private readonly AppStore _appStore;
    
    public bool CreatingPage1Visible { get; set; } = false;
    public bool CreatingPage2Visible { get; set; } = true;
    
    //Attributes
    [ObservableProperty]
    private bool _irisAndSodiumVisible;
    [ObservableProperty]
    private bool _InstallPerformanceMods;
    [ObservableProperty]
    private string _selectedIcon;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SuggestedName))]
    private string _selectedGameVersion;
    [ObservableProperty]
    private string _installationName;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SuggestedName))]
    private string _selectedModLoader;
    [ObservableProperty]
    private string _selectedLoaderVersion;
    [ObservableProperty]
    private bool _useGlobalGameSettings;
    [ObservableProperty]
    private bool _useGlobalBackupSettings;
    [ObservableProperty]
    private bool _isCreatingInstance;
    [ObservableProperty]
    private IsolationType _selectedIsolation = IsolationType.Global;
    [ObservableProperty]
    private int _selectedMaxRam;
    [ObservableProperty]
    private bool _isGameFullscreen;
    [ObservableProperty]
    private string _selectedResolution;
    [ObservableProperty]
    private bool _isEnableAutoBackups;
    [ObservableProperty]
    private BackupFrequency _selectedBackupFrequency;
    [ObservableProperty]
    private int _maxBackupCount;
    [ObservableProperty]
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
    
    public InstanceCreationVM(IGameVersionService versionService, 
        InstancesStore instancesStore, 
        SettingsStore settingsStore,
        AppStore appStore)
    {
        _versionService = versionService;
        
        _instancesStore = instancesStore;
        _settingsStore = settingsStore;
        _appStore = appStore;
        
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
        
        InstallPerformanceMods = false;
        
        
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
            
            if (type == GameLoaderType.Vanilla || string.IsNullOrEmpty(SelectedGameVersion))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => LoaderVersions.Clear());
                return;
            }
            
            var loadedVersions = await _versionService.GetLoaderVersionsAsync(type, SelectedGameVersion);
            var versionList = loadedVersions.ToList();
            
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
            Id = Guid.NewGuid().ToString(),
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
                GameResolution = UseGlobalGameSettings ? null : (IsGameFullscreen ? "Auto" : SelectedResolution),
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
            },
            RequestPerformanceMods = InstallPerformanceMods
        };
        
        WeakReferenceMessenger.Default.Send(new InstanceCreatedMessage(newInstance));
        RequestClose?.Invoke();
        IsCreatingInstance = false;
    }

    private void RefreshPerfomanceModsVisibility()
    {
        if (SelectedModLoader == "Quilt" ||
            SelectedModLoader == "NeoForge" ||
            SelectedModLoader == "Fabric")
        {
            if (SelectedIsolation != IsolationType.Global) IrisAndSodiumVisible = true;
            else IrisAndSodiumVisible = false;
        }
        else
        {
            IrisAndSodiumVisible = false;
        }
    }
    
    
    //Getters and Setters
    
    partial void OnUseGlobalGameSettingsChanged(bool value)
    {
        if (value) RefreshGlobalGameSettings();
    }
    
    partial void OnUseGlobalBackupSettingsChanged(bool value)
    {
        if (value) RefreshGlobalBackupSettings();
    }
    
    partial void OnSelectedGameVersionChanged(string value)
    {
        _ = RefreshLoaderVersions(); 
    }

    partial void OnSelectedIsolationChanged(IsolationType value)
    {
        RefreshPerfomanceModsVisibility();
    }
    
    public string SuggestedName
    {
        get
        {
            if (string.IsNullOrEmpty(SelectedGameVersion)) return "New Installation";
            return $"{SelectedModLoader} {SelectedGameVersion}";
        }
    }
    
    partial void OnSelectedModLoaderChanged(string value)
    {
        if (value == "Vanilla")
        {
            SelectedIsolation = IsolationType.Global;
        }
        else
        {
            SelectedIsolation = IsolationType.Full;
        }
        _ = RefreshGameVersions();
        _ = RefreshLoaderVersions();
        RefreshPerfomanceModsVisibility();
    }
}