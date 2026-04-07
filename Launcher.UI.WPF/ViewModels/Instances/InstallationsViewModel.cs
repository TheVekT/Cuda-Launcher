using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Enums;
using Launcher.Core.Messages;
using Launcher.Core.Models;
using Launcher.Core.Services.Game;
using Launcher.Core.Services.IO;
using Launcher.Core.Services.System;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Resources.Overlay;
using Launcher.UI.WPF.Resources.Overlay.Menus;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;
using Launcher.UI.WPF.ViewModels.Settings;

namespace Launcher.UI.WPF.ViewModels.Instances;

public class InstallationsViewModel : IRecipient<GameLaunchStateMessage>,
    IRecipient<InstanceCreatedMessage>,
    IRecipient<InstanceUpdatedMessage>
{
    //Services
    private readonly IGameVersionService _versionService;
    private readonly IInstanceService _instanceService;
    private readonly IInstanceFileSystemService _instanceFileSystemService;
    private readonly ILauncherPathsService _pathsService;
    private readonly IOverlayService _overlayService;

    //Stores
    private readonly InstancesStore _instancesStore;
    private readonly SettingsStore _settingsStore;
    private readonly AppStore _appStore;

    //Attributes
    private readonly string _instancesFilePath;

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
        ILauncherPathsService pathsService,
        IOverlayService overlayService,
        InstancesStore instancesStore,
        SettingsStore settingsStore,
        AppStore appStore)
    {
        _versionService = versionService;
        _instanceService = instanceService;
        _instanceFileSystemService = instanceFileSystemService;
        _pathsService = pathsService;
        _overlayService = overlayService;
        _instancesStore = instancesStore;
        _settingsStore = settingsStore;
        _appStore = appStore;
        _instancesFilePath = Path.Combine(_pathsService.InstancesDirectory, "instances.json");


        DeleteInstanceCommand = new RelayCommand(async o => await DeleteInstance(o as MinecraftInstance));
        OpenSettingsCommand = new RelayCommand(async o => await OpenSettings(o as MinecraftInstance));

        OpenAddVersionCommand = new RelayCommand(async o => 
        {
            var InstanceVM = new InstanceCreationVM(_versionService, _instancesStore, _settingsStore, _appStore);
            InstanceVM.RequestClose += () => 
            {
                _overlayService.Close();
            };
            _overlayService.Show(InstanceVM);
            await InstanceVM.InitializeAsync();
        });
        
        OpenInstanceFolderCommand = new RelayCommand(o => ExecuteOpenInstanceFolder());

        OpenModsFolderCommand = new RelayCommand(o =>
        {
            if (_instancesStore.SelectedInstance != null)
                _instanceFileSystemService.OpenInstanceModsFolder(_instancesStore.SelectedInstance);
        });
        OpenRootFolderCommand = new RelayCommand(o => _instanceFileSystemService.OpenRootMinecraftFolder());
        
        WeakReferenceMessenger.Default.RegisterAll(this);
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

            bool isFirstLaunch = !File.Exists(_instancesFilePath);

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
        if (instance == _instancesStore.SelectedInstance && _appStore.IsCurrentInstanceProcessing) return;
        var InstanceSettingsVM = new InstanceSettingsVM(instance, _versionService, _instancesStore, _settingsStore);
        InstanceSettingsVM.RequestClose += () => 
        {
            _overlayService.Close();
        };
        _overlayService.Show(InstanceSettingsVM);
        await InstanceSettingsVM.InitializeAsync();
    }

    private async Task DeleteInstance(MinecraftInstance instance)
    {
        if (instance == _instancesStore.SelectedInstance && _appStore.IsCurrentInstanceProcessing) return;
        var confirmVm = new ConfirmVM(
            string.Format(LocalizationService.Instance["Confirmation.DeleteInstanceTitle"], instance.Name), 
            string.Format(LocalizationService.Instance["Confirmation.DeleteInstanceMessage"], instance.Name),
            ConfirmButtons.Delete);

        _overlayService.Show(confirmVm);
        _overlayService.SetClosable(false);
        bool isConfirmed = await confirmVm.WaitAsync();
        _overlayService.SetClosable(true);
        _overlayService.Close();
        
        if (!isConfirmed)
        {
            return;
        }
        _instancesStore.DeleteInstance(instance);
    }
    

    public void Receive(GameLaunchStateMessage message)
    {
        if (message.IsRunning)
        {
            _instancesStore.ApplySort();
            _instanceService.SaveInstances(_instancesStore.Instances);
        }
    }

    public async void Receive(InstanceCreatedMessage message)
    {
        if (message?.Instance == null)
            return;
        try
        {
            await _instanceFileSystemService.InitializeOnCreation(message.Instance);

            void UpdateUiState()
            {
                _instancesStore.Instances.Add(message.Instance);
                _instancesStore.ApplySort();

                if (!_appStore.IsCurrentInstanceProcessing)
                    _instancesStore.SelectedInstance = message.Instance;
            }

            var dispatcher = System.Windows.Application.Current?.Dispatcher;

            if (dispatcher != null && !dispatcher.CheckAccess())
                await dispatcher.InvokeAsync(UpdateUiState);
            else 
                UpdateUiState();
            
            _instanceService.SaveInstances(_instancesStore.Instances);
            Debug.WriteLine($"Created instance: {message.Instance.Name}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[InstallationsVM] Error creating new instance: {ex.Message}");
        }
    }

    public void Receive(InstanceUpdatedMessage message)
    {
        try
        {
            _instanceService.SaveInstances(_instancesStore.Instances);
            _instancesStore.ApplySort();
            Debug.WriteLine($"Modified Instance: {message.Instance.Name}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[InstallationsVM] Error updating instance: {e.Message}");
        }
    }
    
}