using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Common.Enums;
using Launcher.Core.Game.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.Infrastructure.Assets.Abstractions;
using Launcher.UI.WPF.Helpers.Collections;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Models.Instances;
using Launcher.UI.WPF.Services.Windows.Abstractions;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels.Instances;

public partial class AddInstanceViewModel: ObservableObject
{
    //Services
    private readonly IGameVersionService _versionService;
    private readonly IDispatcherService _dispatcherService;
    private readonly IIconsService _iconsService;
    
    //Stores
    private readonly InstancesStore _instancesStore;
    private readonly SettingsStore _settingsStore;
    
    public bool CreatingPage1Visible { get; set; } = false;
    public bool CreatingPage2Visible { get; set; } = true;
    
    //Attributes
    public List<ModLoaderItem> AvailableLoaders { get; } = new()
    {
        new("Vanilla", "150px-Grass_Block_JE7_BE6.png"),
        // new("OptiFine", "optifine-default.png"),
        new("Forge", "forge-default.png"),
        new("NeoForge", "neoforge-default.png"),
        new("Fabric", "fabric-default.png"),
        new("Quilt", "quilt-default.png")
    };

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
    private ModLoaderItem _selectedModLoader;
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
    
    [ObservableProperty]
    private bool _isViewReady;
    
    //Collections
    public ObservableRangeCollection<string> GameVersions { get; } = new(); 
    public ObservableRangeCollection<string> LoaderVersions { get; } = new();
    
    //public properties
    public InstancesStore InstancesStore => _instancesStore;
    public SettingsStore SettingsStore => _settingsStore;
    
    public AddInstanceViewModel(IGameVersionService versionService,
        IDispatcherService dispatcherService,
        IIconsService iconsService,
        InstancesStore instancesStore, 
        SettingsStore settingsStore)
    {
        _versionService = versionService;
        _dispatcherService = dispatcherService;
        _iconsService = iconsService;
        
        _instancesStore = instancesStore;
        _settingsStore = settingsStore;
    }
    
    public async Task InitializeAsync()
    {
        IsViewReady = false;
        
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
        SelectedModLoader = AvailableLoaders.FirstOrDefault(l => l.Name == "Vanilla") 
                            ?? AvailableLoaders.FirstOrDefault();
        CreatingPage1Visible = true;
        CreatingPage2Visible = false;
        if (_instancesStore.IconList.Count > 0){
            SelectedIcon = _instancesStore.IconList.FirstOrDefault();
        }
        
        await Task.Delay(10);
        
        await RefreshGameVersions();
        
        IsViewReady = true;
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
            var type = GetLoaderType(SelectedModLoader.Name);
            
            if (type == GameLoaderType.Vanilla || string.IsNullOrEmpty(SelectedGameVersion))
            {
                _dispatcherService.Invoke(() => 
                {
                    LoaderVersions.Clear();
                    SelectedLoaderVersion = null;
                });
                return;
            }
            
            var loadedVersions = await _versionService.GetLoaderVersionsAsync(type, SelectedGameVersion);
            var versionList = loadedVersions.ToList();
            
            var recommendedVersion = await _versionService.GetRecommendedLoaderVersionAsync(type, SelectedGameVersion);

            _dispatcherService.Invoke(() => 
            {
                LoaderVersions.ReplaceRange(versionList);
                SelectedLoaderVersion = recommendedVersion ?? LoaderVersions.FirstOrDefault();
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
            _dispatcherService.Invoke(() => SelectedGameVersion = null);

            GameLoaderType type = GetLoaderType(SelectedModLoader.Name);
            
            var loadedVersions = await _versionService.GetGameVersionsByTypeAsync(type, _settingsStore.IsEnableSnapshots);
            var versionList = loadedVersions.ToList(); 

            _dispatcherService.Invoke(() => 
            {
                GameVersions.ReplaceRange(versionList);
                SelectedGameVersion = GameVersions.FirstOrDefault();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VM] Ошибка RefreshGameVersions: {ex.Message}");
        }
    }
    
    private GameLoaderType GetLoaderType(string? uiName)
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
    
    private async Task Create()
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
                ? _iconsService.GetIconName(SelectedIcon) 
                : _iconsService.GetIconName(_instancesStore.IconList.FirstOrDefault() ?? ""),
            GameVersion = SelectedGameVersion,
            LoaderVersion = (SelectedModLoader.Name == "Vanilla") ? null : SelectedLoaderVersion,
            LoaderType = GetLoaderType(SelectedModLoader.Name),
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
        IsCreatingInstance = false;
    }

    private void RefreshPerfomanceModsVisibility()
    {
        var loaderName = SelectedModLoader.Name;
        if (loaderName == "Quilt" ||
            loaderName == "NeoForge" ||
            loaderName == "Fabric")
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
            return $"{SelectedModLoader.Name} {SelectedGameVersion}";
        }
    }
    
    partial void OnSelectedModLoaderChanged(ModLoaderItem value)
    {
        if (value.Name == "Vanilla")
            SelectedIsolation = IsolationType.Global;
        else
            SelectedIsolation = IsolationType.Full;
        
        _ = RefreshGameVersions();
        _ = RefreshLoaderVersions();
        RefreshPerfomanceModsVisibility();
    }
    
    //Commands
    [RelayCommand]
    private void ToggleCreatingPage()
    {
        _dispatcherService.Invoke(() =>
        {
            CreatingPage1Visible = !CreatingPage1Visible;
            CreatingPage2Visible = !CreatingPage2Visible;
            OnPropertyChanged(nameof(CreatingPage1Visible));
            OnPropertyChanged(nameof(CreatingPage2Visible));
        });
    }

    [RelayCommand]
    private void CloseSelf() =>
        WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
    
    [RelayCommand]
    private async Task CreateInstance() =>
        await Create();

    [RelayCommand]
    private void SelectIcon()
    {
        _instancesStore.SelectIconFromFileDialog();
    }
    
    [RelayCommand]
    private void DropIcon(string[]? files) =>
        _instancesStore.HandleIconDrop(files);
}