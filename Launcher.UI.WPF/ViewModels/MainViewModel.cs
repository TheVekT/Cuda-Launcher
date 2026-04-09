using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Enums;
using Launcher.Core.Messages;
using Launcher.Core.Models;
using Launcher.Core.Services.Game;
using Launcher.Core.Services.Integrations;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;
using Launcher.UI.WPF.ViewModels.Accounts;
using Launcher.UI.WPF.ViewModels.Game;
using Launcher.UI.WPF.ViewModels.Instances;
using Launcher.UI.WPF.ViewModels.Settings;

namespace Launcher.UI.WPF.ViewModels;

public partial class MainViewModel : ObservableObject,
    IRecipient<GameLaunchStateMessage>,
    IRecipient<GameLaunchProgressMessage>,
    IRecipient<LaunchGameRequestMessage>,
    IRecipient<CloseOverlayMessage>
{
    //Services
    private readonly IGameVersionService _versionService;
    private readonly ILaunchService _launchService;
    private readonly IDiscordService _discordService;
    private readonly ImportOrchestratorService _importOrchestratorService;
    private readonly IOverlayService _overlayService;
    private readonly NavigationService _navigationService;
    private readonly IDispatcherService _dispatcherService;

    //Stores
    private readonly LoginStore _loginStore;
    private readonly SettingsStore _settingsStore;
    private readonly InstancesStore _instancesStore;
    private readonly AppStore _appStore;
    
    //ViewModels
    private PlayViewModel _playVM { get; }
    private InstallationsViewModel _installationsVM { get; }
    private SkinsViewModel _skinsVM { get; }
    private LoginVM _loginVM { get; }
    private SettingsVM _settingsVM { get; }
    
    
    //Public ViewModels 
    public SettingsVM SettingsVM => _settingsVM;
    public LoginVM LoginVM => _loginVM;
    public PlayViewModel PlayVM => _playVM;
    public InstallationsViewModel InstallationsVM => _installationsVM;
    public SkinsViewModel SkinsVM => _skinsVM;
    
    public InstancesStore InstancesStore => _instancesStore;
    
    //Atributes
    private Process? _currentGameProcess;
    [ObservableProperty]
    private bool _isEnabledInstancesComboBox = true;
    //public attributes
    public AppStore AppStore => _appStore;

    public async Task InitializeAsync()
    {
        await _installationsVM.InitializeAsync();
        await _loginStore.RefreshAllAccountsAsync();
        _ = Task.Run(async () => await _versionService.GetGameVersionsByTypeAsync(GameLoaderType.Vanilla));
    }

    public MainViewModel(
        IGameVersionService versionService,
        ILaunchService launchService,
        IDiscordService discordService,
        ImportOrchestratorService importOrchestratorService,
        IOverlayService overlayService,
        NavigationService navigationService,
        IDispatcherService dispatcherService,
        LoginStore loginStore,
        SettingsStore settingsStore,
        InstancesStore instancesStore,
        AppStore appStore,
        PlayViewModel playVM, 
        InstallationsViewModel installationsVM, 
        SkinsViewModel skinsVM,
        LoginVM loginVM,
        SettingsVM settingsVM)
    {
        _versionService = versionService;
        _launchService = launchService;
        _discordService = discordService;
        _importOrchestratorService = importOrchestratorService;
        _overlayService = overlayService;
        _navigationService = navigationService;
        _dispatcherService = dispatcherService;

        //Stores
        _loginStore = loginStore;
        _settingsStore = settingsStore;
        _instancesStore = instancesStore;
        _appStore = appStore;
        
        //ViewModels
        _playVM = playVM;
        _installationsVM = installationsVM;
        _skinsVM = skinsVM;
        _loginVM = loginVM;
        _settingsVM = settingsVM;
        
        
        _discordService.Initialize(Core.Constants.DiscordAppId);
        _overlayService.RegisterOverlaySetter(view => _appStore.CurrentOverlayView = view);
        _navigationService.RegisterNavigationHandler(view => AppStore.CurrentView = view);
        
        _navigationService.Navigate(_playVM);
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }
    
    private async Task HandlePlayButtonPress()
    {
        if (_appStore.IsGameRunning)
        {
            await CloseGameProcess();
        }
        else if (_appStore.IsDownloading)
        {
            return; 
        }
        else
        {
            await LaunchGame(); 
        }
    }
    
    private async Task CloseGameProcess()
    {
        if (_currentGameProcess != null && !_currentGameProcess.HasExited)
        {
            try
            {
                _currentGameProcess.Kill(); 
                await _currentGameProcess.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing game process: {ex}");
            }
        }
    }
    
    private async Task LaunchGame()
    {
        if (_appStore.IsDownloading) return;
        if (_appStore.IsGameRunning) return;

        if (_instancesStore.SelectedInstance == null) return;
        if (_loginStore.CurrentAccount == null) 
        { 
            OpenLogin(); 
            return;
        }

        try
        {
            Console.WriteLine("Launching game...");
            IsEnabledInstancesComboBox = false;
        
            var globalSettings = new GlobalLaunchSettings
            {
                MaxRamMb = _settingsStore.SelectedMaxRam,
                IsFullscreen = _settingsStore.IsGameFullScreen,
                Resolution = _settingsStore.IsGameFullScreen ? "Auto" : _settingsStore.SelectedResolution
            };

            _currentGameProcess = await _launchService.LaunchGameAsync(_instancesStore.SelectedInstance,
                _loginStore.CurrentAccount, globalSettings);

            await _currentGameProcess.WaitForExitAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void HandleDragEnter()
    {
        if (_appStore.CurrentOverlayView != null) return; 
        _appStore.IsDragDropActive = true;
    }

    private void HandleDragLeave()
    {
        _appStore.IsDragDropActive = false;
    }

    private async Task HandleDropAsync(string[]? files)
    {
        _appStore.IsDragDropActive = false;
        
        if (files != null && files.Length > 0)
        {
            using var cts = new CancellationTokenSource();
            
            var progressVM = new ProgressVM(
                onHide: () => _appStore.IsOverlayVisible = false, 
                onCancel: () => cts.Cancel()                      
            )
            {
                Title = LocalizationService.Instance["ProgressMenu.ImportingFiles.Title"],
                Message = String.Format(LocalizationService.Instance["ProgressMenu.ImportingFiles.DescriptionPreparing"], files.Length),
                IsIndeterminate = false,
                ProgressValue = 0,
                ProgressText = "0%"
            };

            _overlayService.Show(progressVM);

            try
            {
                var progressHandler = new Progress<(double Percent, string FileName)>(data => 
                {
                    progressVM.Report(data.Percent);
                    progressVM.Message = String.Format(LocalizationService.Instance["ProgressMenu.ImportingFiles.Description"], data.FileName);
                });
                
                int successCount = await _importOrchestratorService.ProcessDroppedFilesAsync(
                    files, 
                    _instancesStore.SelectedInstance, 
                    progressHandler, 
                    cts.Token);

                if (successCount > 0)
                {
                    var title = LocalizationService.Instance["Success.SuccessImport"];
                    var desc = String.Format(LocalizationService.Instance["Success.SuccessImportDesc"], successCount, files.Length);
                    NotificationService.Instance.ShowSuccess(title, desc);
                }
            }
            catch (OperationCanceledException)
            {
                var title = LocalizationService.Instance["Info.ImportCanceledTitle"];
                var desc = LocalizationService.Instance["Info.ImportCanceledDesc"];
                NotificationService.Instance.ShowInfo(title, desc);
            }
            finally
            {
                _overlayService.Close();
            }
        }
    }

    public void Receive(GameLaunchProgressMessage message)
    {
        // nothing there :D
    }

    public void Receive(GameLaunchStateMessage message)
    {
        _dispatcherService.Invoke(() =>
        {
            if (message.IsRunning)
            {
                Console.WriteLine("Game started!");
                if (!_settingsStore.IsKeepLauncherOpen)
                    WeakReferenceMessenger.Default.Send(new LauncherVisibilityMessage(false));
            }
            else 
            {
                IsEnabledInstancesComboBox = true;
                _currentGameProcess = null;
                WeakReferenceMessenger.Default.Send(new LauncherVisibilityMessage(true));
            }
        });
    }
    
    public void Receive(LaunchGameRequestMessage message)
    {
        _dispatcherService.InvokeAsync(async () => 
        {
            await HandlePlayButtonPress();
        });
    }

    public void Receive(CloseOverlayMessage message)
    {
        _overlayService.Close();
    }
    
    //Commands
    [RelayCommand]
    private void Launch() =>
        WeakReferenceMessenger.Default.Send(new LaunchGameRequestMessage());
    
    
    [RelayCommand]
    private void Navigate(string pageName)
    {
        switch (pageName)
        {
            case "Play": _navigationService.Navigate(_playVM); break;
            case "Installations": _navigationService.Navigate(_installationsVM); break;
            case "Skins": _navigationService.Navigate(_skinsVM); break;
        }
    }

    [RelayCommand]
    private void OpenSettings() => 
        _overlayService.Show(_settingsVM);
    
    [RelayCommand]
    private void CloseOverlay() => 
        WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
    
    
    [RelayCommand]
    private void OpenLogin()
    {
        _loginVM.IsAddAccPageOpen = !_loginStore.IsLoggedIn; 
        _overlayService.Show(_loginVM);
    }
    
    [RelayCommand]
    private void DragEnter()
    {
        HandleDragEnter();
    }
    
    [RelayCommand]
    private void DragLeave()
    {        
        HandleDragLeave();
    }
    
    [RelayCommand]
    private async Task Drop(string[]? files)
    {        
        await HandleDropAsync(files);
    }
}