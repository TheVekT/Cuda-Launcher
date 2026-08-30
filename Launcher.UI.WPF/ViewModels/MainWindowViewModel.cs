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
using Launcher.Core.Game.Exceptions;
using Launcher.Core.Game.Models;
using Launcher.Core.Instances.Abstractions;
using Launcher.Core.Instances.Models;
using Launcher.Core.System.Abstractions;
using Launcher.Infrastructure.Integrations.Abstractions;
using Launcher.UI.WPF.Helpers.Localization;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Models.Shell;
using Launcher.UI.WPF.Services.Customization;
using Launcher.UI.WPF.Services.Shell.Abstractions;
using Launcher.UI.WPF.Services.Windows.Abstractions;
using Launcher.UI.WPF.Stores;
using Launcher.UI.WPF.ViewModels.Common;
using Launcher.UI.WPF.ViewModels.Config;
using Launcher.UI.WPF.ViewModels.Game;
using Launcher.UI.WPF.ViewModels.Identity;
using Launcher.UI.WPF.ViewModels.Instances;

namespace Launcher.UI.WPF.ViewModels;

public partial class MainWindowViewModel : ObservableObject,
    IRecipient<LaunchGameRequestMessage>,
    IRecipient<CloseOverlayMessage>,
    IRecipient<CloseNotificationMessage>
{
    //Services
    private readonly IGameVersionService _versionService;
    private readonly ILaunchService _launchService;
    private readonly IInstanceBackupService _backupService;
    private readonly IImportOrchestratorService _importOrchestratorService;
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
    // ReSharper disable once NotAccessedField.Local
    private readonly SkinsStore _skinsStore;
    
    //ViewModels
    private readonly PlayViewModel _playVm;
    private readonly InstallationsViewModel _installationsVm;
    private readonly SkinsViewModel _skinsVm;
    private readonly LoginViewModel _loginViewModel;
    private readonly SettingsViewModel _settingsViewModel;
    
    
    //Public ViewModels 
    public SettingsViewModel SettingsViewModel => _settingsViewModel;
    public LoginViewModel LoginViewModel => _loginViewModel;
    public PlayViewModel PlayVm => _playVm;
    public InstallationsViewModel InstallationsVm => _installationsVm;
    public SkinsViewModel SkinsVm => _skinsVm;
    
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
        await _installationsVm.InitializeAsync();
        await _identityStore.RefreshAllAccountsAsync();
        await _skinsVm.SyncWithMojangAsync(_identityStore.CurrentAccount?.AccessToken);
        _ = Task.Run(async () => await _versionService.GetGameVersionsByTypeAsync(GameLoaderType.Vanilla));

        bool isOnline = await _connectivityService.CheckInternetAccessAsync();
        if (!isOnline)
        {
            var title = LocalizableText.Key(LocKey.Warnings_NoInternetTitle);
            var desc = LocalizableText.Key(LocKey.Warnings_NoInternetDesc);
            _notificationService.ShowWarning(title, desc);
        }
    }

    public MainWindowViewModel(
        IGameVersionService versionService,
        ILaunchService launchService,
        IInstanceBackupService backupService,
        IDiscordService discordService,
        IImportOrchestratorService importOrchestratorService,
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
        PlayViewModel playVm, 
        InstallationsViewModel installationsVm, 
        SkinsViewModel skinsVm,
        LoginViewModel loginViewModel,
        SettingsViewModel settingsViewModel)
    {
        _versionService = versionService;
        _launchService = launchService;
        _backupService = backupService;
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
        _playVm = playVm;
        _installationsVm = installationsVm;
        _skinsVm = skinsVm;
        _loginViewModel = loginViewModel;
        _settingsViewModel = settingsViewModel;
        
        
        discordService.Initialize(Infrastructure.Common.Constants.DiscordAppId);
        _overlayService.RegisterOverlaySetter(view => _appStore.CurrentOverlayView = view);
        _navigationService.RegisterNavigationHandler(view => AppStore.CurrentView = view);
        _launchService.GameCrashed += OnGameCrashed;
        
        _navigationService.Navigate(_playVm);
        
        WeakReferenceMessenger.Default.RegisterAll(this);
    }
    
    private void OnGameCrashed(MinecraftInstance instance, GameCrashReport report)
    {
        var crashVm = new CrashViewModel(_clipboardService, report.ExitCode, report.StackTrace, report.CrashReportFilePath);
        _overlayService.Show(crashVm);
    }
    
    private async Task HandlePlayButtonPress()
    {
        if (_appStore.IsGameRunning)
        {
            _launchService.GameCrashed -= OnGameCrashed;
            await CloseGameProcess();
            _launchService.GameCrashed += OnGameCrashed;
        }
        else if (_appStore.IsLoading)
        {
            // Do nothing, the game is loading / launching
        }
        else
        {
            await LaunchGameAsync(); 
        }
    }
    
    private async Task CloseGameProcess()
    {
        if (_currentGameProcess is { HasExited: false })
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
        if (_appStore.IsLoading || _appStore.IsGameRunning) return;

        if (_instancesStore.SelectedInstance == null) return;
        if (_identityStore.CurrentAccount == null) 
        { 
            OpenLogin(); 
            return;
        }

        try
        {
            var targetInstance = _instancesStore.SelectedInstance;
            IsEnabledInstancesComboBox = false;
            _appStore.IsLoading = true;

            // 1. Backups Phase
            var globalBackupSettings = new GlobalBackupSettings(
                savesMaxBackups: _settingsStore.MaxBackupCount,
                savesBackupFrequency: _settingsStore.SelectedBackupFrequency
            );

            bool shouldBackup = (targetInstance.BackupSettings.SavesBackupSettings == BackupPolicy.ForceOn ||
                                (targetInstance.BackupSettings.SavesBackupSettings != BackupPolicy.ForceOff && _settingsStore.IsEnableAutoBackups))
                && _backupService.IsBackupDue(targetInstance, globalBackupSettings)
                && _backupService.HasSavesToBackup(targetInstance);

            if (shouldBackup)
            {
                Console.WriteLine("Creating backup before launch...");
                _appStore.LoadingProgress = 0;
                _appStore.LoadingStatusText = "Making backups...";

                var backupProgress = new Progress<InstanceBackupProgress>(p =>
                {
                    _appStore.LoadingProgress = p.Percent;
                    _appStore.LoadingStatusText = p.StatusText;
                });

                var backupResult = await _backupService.CreateBackupAsync(
                    targetInstance, 
                    _instancesStore.Instances, 
                    globalBackupSettings, 
                    backupProgress);

                if (backupResult.IsFailed)
                {
                    Debug.WriteLine($"[Backup Warning] Backup failed: {backupResult.Errors.FirstOrDefault()?.Message}");
                }
                
            }

            // 2. Launch Phase
            Console.WriteLine("Launching game...");
            _appStore.LoadingProgress = 0;
            _appStore.LoadingStatusText = "Preparing...";
        
            var globalSettings = new GlobalLaunchSettings
            (
                allocatedMemory : _settingsStore.SelectedMaxRam,
                fullscreen : _settingsStore.IsGameFullScreen,
                gameResolution : _settingsStore.IsGameFullScreen ? "Auto" : _settingsStore.SelectedResolution,
                jvmArgs : _settingsStore.JvmArguments
            );

            var progress = new Progress<GameLaunchProgressMessage>(p =>
            {
                _appStore.LoadingProgress = p.Percent;
                _appStore.LoadingStatusText = p.Status;
            });

            var result = await _launchService.LaunchGameAsync(targetInstance,
                _identityStore.CurrentAccount, globalSettings, progress);

            if (result.IsSuccess)
            {
                _currentGameProcess = result.Value.Process;
                _appStore.IsGameRunning = true;
                _appStore.IsLoading = false;
                
                if (result.Value.IsPerformanceModsInstalled == false || result.Value.IsEssentialApisInstalled == false)
                {
                    var title = LocalizableText.Key(LocKey.Errors_CantInstallMods_Title);
                    var desc = LocalizableText.Key(LocKey.Errors_CantInstallMods_Desc, targetInstance.Name);
                    _notificationService.ShowError(title, desc);
                }

                if (!string.IsNullOrEmpty(result.Value.SkippedGlobalJvmArguments))
                {
                    _notificationService.ShowWarning(
                        LocalizableText.Key(LocKey.Warnings_InvalidGlobalJvmArgs_Title), 
                        LocalizableText.Key(LocKey.Warnings_InvalidGlobalJvmArgs_Desc));
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
                        LocalizableText.Key(LocKey.Errors_CantInstallVersion_Title),
                        LocalizableText.Key(LocKey.Errors_CantInstallVersion_Desc, targetInstance.Name));
                }
                else if (rootException is InvalidJvmArgumentsException jvmEx)
                {
                    Debug.WriteLine($"[Launch Error] Invalid JVM arguments: {jvmEx.Message}");
                    _notificationService.ShowError(
                        LocalizableText.Key(LocKey.Errors_InvalidInstanceJvmArgs_Title),
                        LocalizableText.Key(LocKey.Errors_InvalidInstanceJvmArgs_Desc));
                }
                else if (rootException is FileNotFoundException or DirectoryNotFoundException)
                {
                    Debug.WriteLine($"[Launch Error] File or directory not found: {rootException.Message}");
                    _notificationService.ShowError(
                        LocalizableText.Key(LocKey.Errors_LaunchError_Title),
                        LocalizableText.Key(LocKey.Errors_LaunchError_Desc, rootException.Message));
                }
                else
                {
                    var errorMessage = string.Join(Environment.NewLine, result.Errors.Select(x => x.Message));
                    _notificationService.ShowError(
                        LocalizableText.Key(LocKey.Errors_LaunchError_Title),
                        LocalizableText.Key(LocKey.Errors_LaunchError_Desc, errorMessage));
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            _notificationService.ShowError(
                LocalizableText.Key(LocKey.Errors_LaunchError_Title),
                LocalizableText.Key(LocKey.Errors_LaunchError_Desc, ex.Message));
        }
        finally
        {
            await _dispatcherService.InvokeAsync(() =>
            {
                _appStore.IsLoading = false;
                _appStore.IsGameRunning = false;
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
            var cts = new CancellationTokenSource();
            
            var progressVm = new ProgressViewModel(
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

            _overlayService.Show(progressVm);

            try
            {
                var progressHandler = new Progress<(double Percent, string FileName)>(data => 
                {
                    progressVm.Report(data.Percent);
                    progressVm.Message = String.Format(LocalizationService.Instance[LocKey.ProgressMenu_ImportingFiles_Description], data.FileName);
                });
                
                int successCount = await _importOrchestratorService.ProcessDroppedFilesAsync(
                    files, 
                    _instancesStore.SelectedInstance, 
                    progressHandler, 
                    cts.Token);

                if (successCount > 0)
                {
                    var title = LocalizableText.Key(LocKey.Success_SuccessImport);
                    var desc = LocalizableText.Key(LocKey.Success_SuccessImportDesc, successCount, files.Length);
                    _notificationService.ShowSuccess(title, desc);
                }
            }
            catch (OperationCanceledException)
            {
                var title = LocalizableText.Key(LocKey.Info_ImportCanceledTitle);
                var desc = LocalizableText.Key(LocKey.Info_ImportCanceledDesc);
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
            case "Play": _navigationService.Navigate(_playVm); break;
            case "Installations": _navigationService.Navigate(_installationsVm); break;
            case "Skins": _navigationService.Navigate(_skinsVm); break;
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