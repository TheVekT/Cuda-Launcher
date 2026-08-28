using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Common.Enums;
using Launcher.Core.Game.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.Infrastructure.Assets.Abstractions;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Models;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Services.Abstractions;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels.Instances;

public partial class EditInstanceViewModel : ObservableObject
{
    //Services
    private readonly IGameVersionService _versionService;
    private readonly IDispatcherService _dispatcherService;
    private readonly IIconsService _iconsService;
    
    //Stores
    private readonly InstancesStore _instancesStore;
    private readonly SettingsStore _settingsStore;
    
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
    
    private MinecraftInstance _instance;
    
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
    public ObservableRangeCollection<string> LoaderVersions { get; } = new();
    
    //public properties
    public InstancesStore InstancesStore => _instancesStore;
    public SettingsStore SettingsStore => _settingsStore;
    
    public EditInstanceViewModel(MinecraftInstance instance,
        IGameVersionService versionService,
        IDispatcherService dispatcherService,
        IIconsService iconsService,
        InstancesStore instancesStore, 
        SettingsStore settingsStore)
    {
        _instance = instance;
        
        _versionService = versionService;
        _dispatcherService = dispatcherService;
        _iconsService = iconsService;
        
        _instancesStore = instancesStore;
        _settingsStore = settingsStore;
    }

    public async Task InitializeAsync()
    {
        IsViewReady = false;
        
        InstallationName = _instance.Name;
        SelectedGameVersion = _instance.GameVersion;
        SelectedLoaderVersion = _instance.LoaderType == GameLoaderType.Vanilla ? null : _instance.LoaderVersion;
        SelectedIcon = _instancesStore.IconList.FirstOrDefault(fullPath => 
            _iconsService.GetIconName(fullPath) == _instance.IconPath);
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
        
        string targetLoaderName = _instance.LoaderType.ToString();
        SelectedModLoader = AvailableLoaders.FirstOrDefault(l => l.Name == targetLoaderName) 
                            ?? AvailableLoaders.FirstOrDefault(); 
        
        await Task.Delay(10);
        
        await RefreshLoaderVersions();
        
        IsViewReady = true;
    }
    
    private void SaveNewInstanceSettings()
    {
        var finalName = string.IsNullOrWhiteSpace(InstallationName) 
            ? SuggestedName 
            : InstallationName;

        _instance.Name = finalName;
        _instance.IconPath = !string.IsNullOrEmpty(SelectedIcon) 
            ? _iconsService.GetIconName(SelectedIcon) 
            : _iconsService.GetIconName(_instancesStore.IconList.FirstOrDefault() ?? "");
        _instance.LoaderVersion = (SelectedModLoader.Name == "Vanilla") ? null : SelectedLoaderVersion;
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
        
        WeakReferenceMessenger.Default.Send(new InstanceUpdatedMessage(_instance)); 
        WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
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
            string currentSavedVersion = _selectedLoaderVersion;
            
            _dispatcherService.Invoke(() => 
            {
                LoaderVersions.ReplaceRange(versionList);

                if (!string.IsNullOrEmpty(currentSavedVersion) && LoaderVersions.Contains(currentSavedVersion))
                {
                    SelectedLoaderVersion = currentSavedVersion;
                }
                else
                {
                    SelectedLoaderVersion = LoaderVersions.FirstOrDefault();
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VM] Ошибка RefreshLoaderVersions: {ex.Message}");
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
    
    //Getters and Setters
    partial void OnUseGlobalGameSettingsChanged(bool value)
    {
        if (value) RefreshGlobalGameSettings();
    }
    
    partial void OnUseGlobalBackupSettingsChanged(bool value)
    {
        if (value) RefreshGlobalBackupSettings();
    }

    public string SuggestedName
    {
        get
        {
            if (string.IsNullOrEmpty(SelectedGameVersion)) return "New Installation";
            return $"{SelectedModLoader.Name} {SelectedGameVersion}";
        }
    }
    
    #region Relay Commands
    
    [RelayCommand]
    private void CloseSelf() =>
        WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());

    [RelayCommand]
    private void Save() =>
        SaveNewInstanceSettings();
    
    [RelayCommand]
    private void SelectIcon()
    {
        _instancesStore.SelectIconFromFileDialog();
    }
    
    [RelayCommand]
    private void DropIcon(string[]? files) =>
        _instancesStore.HandleIconDrop(files);
    
    #endregion
}