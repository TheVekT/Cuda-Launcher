using System.ComponentModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Assets.Abstractions;
using Launcher.Core.Common.Enums;
using Launcher.Core.Game.Abstractions;
using Launcher.Core.Instances.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.Core.System.Abstractions;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Services.Abstractions;
using Launcher.UI.WPF.Stores;
using Launcher.UI.WPF.ViewModels.Common;

namespace Launcher.UI.WPF.ViewModels.Instances;

public partial class InstallationsViewModel : ObservableObject,
    IRecipient<InstanceCreatedMessage>,
    IRecipient<InstanceUpdatedMessage>
{
    //Services
    private readonly IGameVersionService _versionService;
    private readonly IInstanceService _instanceService;
    private readonly IInstanceFileSystemService _instanceFileSystemService;
    private readonly IOverlayService _overlayService;
    private readonly IDispatcherService _dispatcherService;
    private readonly IInputService _inputService;
    private readonly IIconsService _iconsService;

    //Stores
    private readonly InstancesStore _instancesStore;
    private readonly SettingsStore _settingsStore;
    private readonly AppStore _appStore;

    //Attributes
    private readonly string _instancesFilePath;
    
    //public properties
    public InstancesStore InstancesStore => _instancesStore;
    public AppStore AppStore => _appStore;
  
    
    public InstallationsViewModel(
        IGameVersionService versionService,
        IInstanceService instanceService,
        IInstanceFileSystemService instanceFileSystemService,
        IOverlayService overlayService,
        IDispatcherService dispatcherService,
        IInputService inputService,
        IIconsService iconsService,
        InstancesStore instancesStore,
        SettingsStore settingsStore,
        AppStore appStore)
    {
        _versionService = versionService;
        _instanceService = instanceService;
        _instanceFileSystemService = instanceFileSystemService;
        _overlayService = overlayService;
        _dispatcherService = dispatcherService;
        _inputService = inputService;
        _iconsService = iconsService;
        _instancesStore = instancesStore;
        _settingsStore = settingsStore;
        _appStore = appStore;
        
        _appStore.PropertyChanged += OnAppStorePropertyChanged;
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
            bool isFirstLaunch = _instancesStore.Instances.Count == 0;

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
    
        if (_inputService.IsShiftPressed)
            _instanceFileSystemService.OpenInstanceModsFolder(_instancesStore.SelectedInstance);
        else
            _instanceFileSystemService.OpenInstanceFolder(_instancesStore.SelectedInstance);
    }
    

    private async Task DeleteInstance(MinecraftInstance instance)
    {
        if (instance == _instancesStore.SelectedInstance && _appStore.IsCurrentInstanceProcessing) return;
        var confirmVm = new ConfirmViewModel(
            string.Format(LocalizationService.Instance["Confirmation.DeleteInstanceTitle"], instance.Name), 
            string.Format(LocalizationService.Instance["Confirmation.DeleteInstanceMessage"], instance.Name),
            ConfirmButtons.Delete);

        _overlayService.Show(confirmVm);
        bool isConfirmed = await confirmVm.WaitAsync();
        _overlayService.Close();
        
        if (!isConfirmed)
        {
            return;
        }
        _instancesStore.DeleteInstance(instance);
    }
    

    private void OnAppStorePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppStore.IsGameRunning) && _appStore.IsGameRunning)
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
            _overlayService.SetClosable(false);
            await _instanceFileSystemService.InitializeOnCreation(message.Instance);

            void UpdateUiState()
            {
                _instancesStore.Instances.Add(message.Instance);
                _instancesStore.ApplySort();

                if (!_appStore.IsCurrentInstanceProcessing)
                    _instancesStore.SelectedInstance = message.Instance;
            }
            await _dispatcherService.InvokeAsync(UpdateUiState);
        
            _instanceService.SaveInstances(_instancesStore.Instances);
            _overlayService.SetClosable(true);
            WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
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
    
    //Commands
    [RelayCommand]
    private async Task DeleteInstance(object parameter) =>
        await DeleteInstance(parameter as MinecraftInstance);
    
    [RelayCommand]
    private async Task OpenSettings(object parameter)
    {
        var instance = parameter as MinecraftInstance;
        if (instance == _instancesStore.SelectedInstance && _appStore.IsCurrentInstanceProcessing) return;
        var InstanceSettingsVM = new EditInstanceViewModel(instance, _versionService, _dispatcherService, _iconsService, _instancesStore, _settingsStore);
        _overlayService.Show(InstanceSettingsVM);
        await InstanceSettingsVM.InitializeAsync();
    }
    
    [RelayCommand]
    private async Task OpenAddVersion()
    {
        var InstanceVM = new AddInstanceViewModel(_versionService, _dispatcherService, _iconsService, _instancesStore, _settingsStore);
        _overlayService.Show(InstanceVM);
        await InstanceVM.InitializeAsync();
    }
    
    [RelayCommand]
    private void OpenInstanceFolder() =>
        ExecuteOpenInstanceFolder();

    [RelayCommand]
    private void OpenModsFolder()
    {
        if (_instancesStore.SelectedInstance != null)
            _instanceFileSystemService.OpenInstanceModsFolder(_instancesStore.SelectedInstance);
    }

    [RelayCommand]
    private void OpenRootFolder() =>
        _instanceFileSystemService.OpenRootMinecraftFolder();
}