using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Sockets;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FluentResults;
using Launcher.Core.Common.Enums;
using Launcher.Core.Common.Messages;
using Launcher.Core.Game.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.Core.Integrations.Abstractions;
using Launcher.Core.System.Abstractions;
using Launcher.Core.UI.Abstractions;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Services.Abstractions;
using Launcher.UI.WPF.Stores;
using Launcher.UI.WPF.ViewModels.Accounts;
using Launcher.UI.WPF.ViewModels.Common;
using Launcher.UI.WPF.ViewModels.Game;
using Launcher.UI.WPF.ViewModels.Instances;
using Launcher.UI.WPF.ViewModels.Settings;

namespace Launcher.UI.WPF.ViewModels;

public partial class MainWindowViewModel : ObservableObject,
    IRecipient<LaunchGameRequestMessage>,
    IRecipient<CloseOverlayMessage>,
    IRecipient<CloseNotificationMessage>
{
    //Services
    private readonly IGameVersionService _versionService;
    private readonly ILaunchService _launchService;
    private readonly IDiscordService _discordService;
    private readonly ImportOrchestratorService _importOrchestratorService;
    private readonly IOverlayService _overlayService;
    private readonly INavigationService _navigationService;
    private readonly IDispatcherService _dispatcherService;
    private readonly INotificationService _notificationService;
    private readonly IClipboardService _clipboardService;
    private readonly IConnectivityService _connectivityService;

    //Stores
    private readonly IdentityStore _identityStore;
    private readonly SettingsStore _settingsStore;
    private readonly InstancesStore _instancesStore;
    private readonly AppStore _appStore;
    private readonly SkinsStore _skinsStore;
    
    //ViewModels
    private readonly PlayViewModel _playVM;
    private readonly InstallationsViewModel _installationsVM;
    private readonly SkinsViewModel _skinsVM;
    private readonly LoginViewModel _loginViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    
    
    //Public ViewModels 
    public SettingsViewModel SettingsViewModel => _settingsViewModel;
    public LoginViewModel LoginViewModel => _loginViewModel;
    public PlayViewModel PlayVM => _playVM;
    public InstallationsViewModel InstallationsVM => _installationsVM;
    public SkinsViewModel SkinsVM => _skinsVM;
    
    public InstancesStore InstancesStore => _instancesStore;
    public IdentityStore IdentityStore => _identityStore;
    
    //Atributes
    private Process? _currentGameProcess;
    [ObservableProperty]
    private bool _isEnabledInstancesComboBox = true;
    //public attributes
    public AppStore AppStore => _appStore;
    
    public INotificationService NotificationService => _notificationService;

    public async Task InitializeAsync()
    {
        await _installationsVM.InitializeAsync();
        await _identityStore.RefreshAllAccountsAsync();
        await _skinsVM.SyncWithMojangAsync(_identityStore.CurrentAccount?.AccessToken);
        _ = Task.Run(async () => await _versionService.GetGameVersionsByTypeAsync(GameLoaderType.Vanilla));

        bool isOnline = await _connectivityService.CheckInternetAccessAsync();
        if (!isOnline)
        {
            var title = LocalizationService.Instance[LocKey.Warnings_NoInternetTitle] ?? "No Internet Connection";
            var desc = LocalizationService.Instance[LocKey.Warnings_NoInternetDesc] ?? "You are currently offline. Some features and online versions will be unavailable.";
            _notificationService.ShowWarning(title, desc);
        }
    }

    public MainWindowViewModel(
        IGameVersionService versionService,
        ILaunchService launchService,
        IDiscordService discordService,
        ImportOrchestratorService importOrchestratorService,
        IOverlayService overlayService,
        INavigationService navigationService,
        IDispatcherService dispatcherService,
        INotificationService notificationService,
        IClipboardService clipboardService,
        IConnectivityService connectivityService,
        IdentityStore identityStore,
        SettingsStore settingsStore,
        InstancesStore instancesStore,
        AppStore appStore,
        SkinsStore skinsStore,
        PlayViewModel playVM, 
        InstallationsViewModel installationsVM, 
        SkinsViewModel skinsVM,
        LoginViewModel loginViewModel,
        SettingsViewModel settingsViewModel)
    {
        _versionService = versionService;
        _launchService = launchService;
        _discordService = discordService;
        _importOrchestratorService = importOrchestratorService;
        _overlayService = overlayService;
        _navigationService = navigationService;
        _dispatcherService = dispatcherService;
        _notificationService = notificationService;
        _clipboardService = clipboardService;
        _connectivityService = connectivityService;

        //Stores
        _identityStore = identityStore;
        _settingsStore = settingsStore;
        _instancesStore = instancesStore;
        _appStore = appStore;
        _skinsStore = skinsStore;
        
        //ViewModels
        _playVM = playVM;
        _installationsVM = installationsVM;
        _skinsVM = skinsVM;
        _loginViewModel = loginViewModel;
        _settingsViewModel = settingsViewModel;
        
        
        _discordService.Initialize(Launcher.Core.Common.Constants.LauncherConstants.DiscordAppId);
        _overlayService.RegisterOverlaySetter(view => _appStore.CurrentOverlayView = view);
        _navigationService.RegisterNavigationHandler(view => AppStore.CurrentView = view);
        _launchService.GameCrashed += OnGameCrashed;
        
        _navigationService.Navigate(_playVM);
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }
    
    private void OnGameCrashed(MinecraftInstance instance, GameCrashReport report)
    {
        var crashVM = new CrashViewModel(_clipboardService, report.ExitCode, report.StackTrace, report.CrashReportFilePath);
        _overlayService.Show(crashVM);
    }
    
    private async Task HandlePlayButtonPress()
    {
        if (_appStore.IsGameRunning)
        {
            _launchService.GameCrashed -= OnGameCrashed;
            await CloseGameProcess();
            _launchService.GameCrashed += OnGameCrashed;
        }
        else if (_appStore.IsDownloading)
        {
            return; 
        }
        else
        {
            await LaunchGameAsync(); 
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
    
    private async Task LaunchGameAsync()
    {
        if (_appStore.IsDownloading) return;
        if (_appStore.IsGameRunning) return;

        if (_instancesStore.SelectedInstance == null) return;
        if (_identityStore.CurrentAccount == null) 
        { 
            OpenLogin(); 
            return;
        }

        try
        {
            Console.WriteLine("Launching game...");
            IsEnabledInstancesComboBox = false;
            _appStore.IsDownloading = true;
        
            var globalSettings = new GlobalLaunchSettings
            {
                MaxRamMb = _settingsStore.SelectedMaxRam,
                IsFullscreen = _settingsStore.IsGameFullScreen,
                Resolution = _settingsStore.IsGameFullScreen ? "Auto" : _settingsStore.SelectedResolution
            };

            var progress = new Progress<GameLaunchProgressMessage>(p =>
            {
                _appStore.DownloadProgress = p.Percent;
                _appStore.DownloadStatusText = p.Status;
            });

            var result = await _launchService.LaunchGameAsync(_instancesStore.SelectedInstance,
                _identityStore.CurrentAccount, globalSettings, progress);

            if (result.IsSuccess)
            {
                _currentGameProcess = result.Value.Process;
                _appStore.IsGameRunning = true;
                _appStore.IsDownloading = false;
                
                if (result.Value.IsPerformanceModsInstalled == false || result.Value.IsEssentialApisInstalled == false)
                {
                    var title = LocalizationService.Instance[LocKey.Errors_CantInstallMods_Title];
                    var desc = LocalizationService.Instance[LocKey.Errors_CantInstallMods_Desc];
                    _notificationService.ShowError(title, string.Format(desc, _instancesStore.SelectedInstance.Name));
                }

                Console.WriteLine("Game started!");
                if (!_settingsStore.IsKeepLauncherOpen)
                    WeakReferenceMessenger.Default.Send(new LauncherVisibilityMessage(false));

                await _currentGameProcess.WaitForExitAsync();
            }
            else if (result.IsFailed)
            {
                var rootException = result.Errors
                    .SelectMany(e => e.Reasons.OfType<ExceptionalError>())
                    .Select(e => e.Exception)
                    .FirstOrDefault();
                if (rootException is HttpRequestException or SocketException)
                {
                    Debug.WriteLine($"[Launch Error] Network error: {rootException.Message}");
                    _notificationService.ShowError(
                        LocalizationService.Instance[LocKey.Errors_CantInstallVersion_Title],
                        string.Format(LocalizationService.Instance[LocKey.Errors_CantInstallVersion_Desc], _instancesStore.SelectedInstance.Name));
                }
                else if (rootException is FileNotFoundException or DirectoryNotFoundException)
                {
                    Debug.WriteLine($"[Launch Error] File or directory not found: {rootException.Message}");
                    _notificationService.ShowError(
                        LocalizationService.Instance[LocKey.Errors_LaunchError_Title],
                        $"Required file or directory not found: {rootException.Message}");
                }
                else
                {
                    var errorMessage = string.Join(Environment.NewLine, result.Errors.Select(x => x.Message));
                    _notificationService.ShowError(
                        LocalizationService.Instance[LocKey.Errors_LaunchError_Title],
                        errorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            _notificationService.ShowError(
                LocalizationService.Instance[LocKey.Errors_LaunchError_Title],
                ex.Message);
        }
        finally
        {
            _dispatcherService.Invoke(() =>
            {
                _appStore.IsGameRunning = false;
                _appStore.IsDownloading = false;
                _currentGameProcess = null;
                IsEnabledInstancesComboBox = true;
                WeakReferenceMessenger.Default.Send(new LauncherVisibilityMessage(true));
            });
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
            
            var progressVM = new ProgressViewModel(
                onHide: () => _appStore.IsOverlayVisible = false, 
                onCancel: () => cts.Cancel()                      
            )
            {
                Title = LocalizationService.Instance[LocKey.ProgressMenu_ImportingFiles_Title],
                Message = String.Format(LocalizationService.Instance[LocKey.ProgressMenu_ImportingFiles_DescriptionPreparing], files.Length),
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
                    progressVM.Message = String.Format(LocalizationService.Instance[LocKey.ProgressMenu_ImportingFiles_Description], data.FileName);
                });
                
                int successCount = await _importOrchestratorService.ProcessDroppedFilesAsync(
                    files, 
                    _instancesStore.SelectedInstance, 
                    progressHandler, 
                    cts.Token);

                if (successCount > 0)
                {
                    var title = LocalizationService.Instance[LocKey.Success_SuccessImport];
                    var desc = String.Format(LocalizationService.Instance[LocKey.Success_SuccessImportDesc], successCount, files.Length);
                    _notificationService.ShowSuccess(title, desc);
                }
            }
            catch (OperationCanceledException)
            {
                var title = LocalizationService.Instance[LocKey.Info_ImportCanceledTitle];
                var desc = LocalizationService.Instance[LocKey.Info_ImportCanceledDesc];
                _notificationService.ShowInfo(title, desc);
            }
            finally
            {
                _overlayService.Close();
            }
        }
    }


    
    public void Receive(LaunchGameRequestMessage message)
    {
        _dispatcherService.InvokeAsync(async () => 
        {
            await HandlePlayButtonPress();
        });
    }

    public void Receive(CloseOverlayMessage message) =>
        _overlayService.Close();
    

    public void Receive(CloseNotificationMessage message) =>
        _notificationService.Remove(message.MessageId);
    
    
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
        _overlayService.Show(_settingsViewModel);
    
    [RelayCommand]
    private void CloseOverlay() => 
        WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
    
    [RelayCommand]
    private void OpenLogin()
    {
        _loginViewModel.IsAddAccPageOpen = !_identityStore.IsLoggedIn; 
        _overlayService.Show(_loginViewModel);
    }
    
    [RelayCommand]
    private void DragEnter() =>
        HandleDragEnter();
    
    
    [RelayCommand]
    private void DragLeave() =>
        HandleDragLeave();
    
    
    [RelayCommand]
    private async Task Drop(string[]? files) =>
        await HandleDropAsync(files);
    
}