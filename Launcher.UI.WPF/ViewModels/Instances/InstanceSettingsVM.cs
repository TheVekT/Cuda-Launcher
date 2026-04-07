using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
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

public partial class InstanceSettingsVM : ObservableObject
{
    //Services
    private readonly IGameVersionService _versionService;
    
    //Stores
    private readonly InstancesStore _instancesStore;
    private readonly SettingsStore _settingsStore;
    
    //Attributes
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
    private string _selectedModLoader;
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
        InstancesStore instancesStore, 
        SettingsStore settingsStore)
    {
        _instance = instance;
        
        _versionService = versionService;
        
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
        
        WeakReferenceMessenger.Default.Send(new InstanceUpdatedMessage(_instance)); 
        RequestClose?.Invoke();
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
            return $"{SelectedModLoader} {SelectedGameVersion}";
        }
    }
}