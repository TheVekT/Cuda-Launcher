using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Launcher.Core.Models;
using Launcher.Core.Services.Game;
using Launcher.Core.Services.IO;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Resources.Overlay;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels;

public class InstallationsViewModel : INotifyPropertyChanged
{
    //Services
    private readonly IGameVersionService _versionService; 
    private readonly IInstanceService _instanceService;
    private readonly IInstanceFileSystemService _instanceFileSystemService;
    
    //Stores
    private readonly InstancesStore _instancesStore;
    private readonly SettingsStore _settingsStore;
    private readonly AppStore _appStore;
    
    //Commands
    public ICommand DeleteInstanceCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenAddVersionCommand { get; }
    public ICommand OpenInstanceFolderCommand { get; }
    public ICommand OpenModsFolderCommand { get; }
    public ICommand OpenRootFolderCommand { get; }
    //Attributes
    
    
    //public properties
    public InstancesStore InstancesStore => _instancesStore;
  
    
    public InstallationsViewModel(
        IGameVersionService versionService, 
        IInstanceService instanceService,
        IInstanceFileSystemService instanceFileSystemService,
        InstancesStore instancesStore,
        SettingsStore settingsStore,
        AppStore appStore)
    {
        _versionService = versionService;
        _instanceService = instanceService;
        _instanceFileSystemService = instanceFileSystemService;
        
        _instancesStore = instancesStore;
        _settingsStore = settingsStore;
        _appStore = appStore;
        
        
        DeleteInstanceCommand = new RelayCommand(async o => await DeleteInstance(o as MinecraftInstance));
        OpenSettingsCommand = new RelayCommand(async o => await OpenSettings(o as MinecraftInstance));

        OpenAddVersionCommand = new RelayCommand(async o => 
        {
            var menu = new AddVersionMenu();
            var InstanceVM = new InstanceCreationVM(_versionService, _instanceService, _instanceFileSystemService, _instancesStore, _settingsStore, _appStore);
            menu.DataContext = InstanceVM; 
            InstanceVM.RequestClose += () => 
            {
                _appStore.CurrentOverlayView = null;
            };
            _appStore.CurrentOverlayView = menu;
            await InstanceVM.InitializeAsync();
        });
        
        OpenInstanceFolderCommand = new RelayCommand(o => ExecuteOpenInstanceFolder());

        OpenModsFolderCommand = new RelayCommand(o =>
        {
            if (_instancesStore.SelectedInstance != null)
                _instanceFileSystemService.OpenInstanceModsFolder(_instancesStore.SelectedInstance);
        });
        OpenRootFolderCommand = new RelayCommand(o => _instanceFileSystemService.OpenRootMinecraftFolder());
        
    }
    
    public async Task InitializeAsync()
    {
        await HandleLatestReleaseAsync();
    }
    
    private async Task HandleLatestReleaseAsync()
    {
        try
        {
            var vanillaVersions = await _versionService.GetVanillaVersionsAsync();
            var latestVersion = vanillaVersions.FirstOrDefault();

            if (string.IsNullOrEmpty(latestVersion)) return;
            
            string instancesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Instances", "instances.json");
            bool isFirstLaunch = !File.Exists(instancesFilePath);

            if (isFirstLaunch) CreateLatestRelease(latestVersion);
            else UpdateLatestRelease(latestVersion);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[InstallationsVM] Ошибка обработки Latest Release: {ex.Message}");
        }
    }

    private void CreateLatestRelease(string latestVersion)
    {
        var latestInstance = new MinecraftInstance
        {
            Id = "LatestRelease",
            Name = "Latest Release",
            GameVersion = latestVersion,
            LoaderType = GameLoaderType.Vanilla,
            IsolationType = IsolationType.Full, 
            IconPath = "logo.png",
            GameSettings = new GameSettings
            {
                GameResolution = null,
                AllocatedMemory = null,
                Fullscreen = null,
                JvmArgs = null
            },
            BackupSettings = new BackupSettings
            {
                SavesBackupSettings = BackupPolicy.Inherit,
                SavesBackupFrequency = null,
                SavesMaxBackups = null,
                LastBackupDate = null
            }
        };
        _instancesStore.Instances.Add(latestInstance);
        _instancesStore.SelectedInstance = latestInstance;
        _instanceService.SaveInstances(_instancesStore.Instances);
    }

    private void UpdateLatestRelease(string latestVersion)
    {
        var latestInstance = _instancesStore.Instances.FirstOrDefault(i => i.Id == "LatestRelease");
        
        if (latestInstance != null && latestInstance.GameVersion != latestVersion)
        {
            latestInstance.GameVersion = latestVersion;
            _instanceService.SaveInstances(_instancesStore.Instances);
        }
    }
    
    private void ExecuteOpenInstanceFolder()
    {
        if (_instancesStore.SelectedInstance == null) return;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            _instanceFileSystemService.OpenInstanceModsFolder(_instancesStore.SelectedInstance);
        else
            _instanceFileSystemService.OpenInstanceFolder(_instancesStore.SelectedInstance);
    }

    private async Task OpenSettings(MinecraftInstance instance)
    {
        if (instance == _instancesStore.SelectedInstance && _appStore.IsCurrentInstanceInProcess) return;
        var menu = new VersionSettingsMenu();
        var InstanceSettingsVM = new InstanceSettingsVM(instance, _versionService, _instanceService, _instanceFileSystemService, _instancesStore, _settingsStore);
        menu.DataContext = InstanceSettingsVM; 
        InstanceSettingsVM.RequestClose += () => 
        {
            _appStore.CurrentOverlayView = null;
        };
        _appStore.CurrentOverlayView = menu;
        await InstanceSettingsVM.InitializeAsync();
    }

    private async Task DeleteInstance(MinecraftInstance instance)
    {
        if (instance == _instancesStore.SelectedInstance && _appStore.IsCurrentInstanceInProcess) return;
        var confirmVm = new ConfirmVM(
            string.Format(LocalizationService.Instance["Confirmation.DeleteInstanceTitle"], instance.Name), 
            string.Format(LocalizationService.Instance["Confirmation.DeleteInstanceMessage"], instance.Name),
            ConfirmButtons.Delete);
        var view = new ConfirmMenu();
        view.DataContext = confirmVm;
        _appStore.CurrentOverlayView = view;
        bool isConfirmed = await confirmVm.WaitAsync();
        _appStore.CurrentOverlayView = null;
        
        if (!isConfirmed)
        {
            return;
        }
        _instancesStore.DeleteInstance(instance);
    }

    
    
    //Getters and setters


    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}