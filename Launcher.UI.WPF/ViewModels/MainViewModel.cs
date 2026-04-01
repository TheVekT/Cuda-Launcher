using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Launcher.Core.Enums;
using Launcher.Core.Models;
using Launcher.Core.Services;
using Launcher.Core.Services.Auth; 
using Launcher.Core.Services.IO;
using Launcher.Core.Services.Game;
using Launcher.Core.Services.Integrations;
using Launcher.Core.Services.System;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Resources.Overlay;
using Launcher.UI.WPF.Resources.Overlay.Menus;
using Launcher.UI.WPF.Resources.Overlay.Notifications;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;
using Launcher.UI.WPF.ViewModels.Accounts;
using Launcher.UI.WPF.ViewModels.Game;
using Launcher.UI.WPF.ViewModels.Instances;
using Launcher.UI.WPF.ViewModels.Settings;
using Microsoft.VisualBasic;

namespace Launcher.UI.WPF.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    //Services
    private readonly ThemeService _themeService;
    private readonly IAuthService _authService; 
    private readonly IAccountStorageService _accountStorage;
    private readonly IGameVersionService _versionService;
    private readonly IInstanceService _instanceService; 
    private readonly IInstanceFileSystemService _instanceFileSystemService;
    private readonly ILaunchService _launchService;
    private readonly ISysInfoService _sysInfoService;
    private readonly IDiscordService _discordService;
    private readonly IDragDropParserService _dragDropParserService;
    private readonly ILauncherPathsService _pathsService;

    //Stores
    private readonly LoginStore _loginStore;
    private readonly SettingsStore _settingsStore;
    private readonly LaunchStore _launchStore;
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
    
    //Atributes
    private object _currentView;
    private bool _showCompactPlayButton;
    private Process? _currentGameProcess;
    private bool _isEnabledInstancesComboBox = true;
    
    //Overlays
    private SettingsMenu _settingsMenu;
    
    //Commands
    public ICommand LaunchCommand { get; }
    public ICommand CloseOverlayCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenLoginCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand DragEnterCommand { get; }
    public ICommand DragLeaveCommand { get; }
    public ICommand DropCommand { get; }
    
    //public attributes
    public AppStore AppStore => _appStore;

    public async Task InitializeAsync()
    {
        await _installationsVM.InitializeAsync();
        await RefreshAllAccountsAsync();
        _ = Task.Run(async () => await _versionService.GetGameVersionsByTypeAsync(GameLoaderType.Vanilla));
    }

    public MainViewModel(
        ThemeService themeService, 
        IAuthService authService, 
        IAccountStorageService accountStorage,
        IGameVersionService versionService,
        IInstanceService instanceService,
        IInstanceFileSystemService instanceFileSystemService,
        ILaunchService launchService,
        ISysInfoService sysInfoService,
        IDiscordService discordService,
        IDragDropParserService dragDropParserService,
        ILauncherPathsService pathsService,
        LoginStore loginStore,
        SettingsStore settingsStore,
        LaunchStore launchStore,
        InstancesStore instancesStore,
        AppStore appStore)
    {
        _themeService = themeService;
        _authService = authService;
        _accountStorage = accountStorage;
        _versionService = versionService;
        _instanceService = instanceService;
        _instanceFileSystemService = instanceFileSystemService;
        _launchService = launchService;
        _sysInfoService = sysInfoService;
        _discordService = discordService;
        _dragDropParserService = dragDropParserService;
        _pathsService = pathsService;

        //Stores
        _loginStore = loginStore;
        _settingsStore = settingsStore;
        _launchStore = launchStore;
        _instancesStore = instancesStore;
        _appStore = appStore;
        
        //ViewModels
        _playVM = new PlayViewModel(_discordService, _settingsStore, _instancesStore, _appStore);
        _installationsVM = new InstallationsViewModel(_versionService, _instanceService, _instanceFileSystemService, _pathsService, _instancesStore, _settingsStore, _appStore);
        _skinsVM = new SkinsViewModel();
        _loginVM = new LoginVM(_authService, _accountStorage,_loginStore);
        _settingsVM = new SettingsVM(_settingsStore, _themeService, _appStore);
        
        //Overlays
        _settingsMenu = new SettingsMenu();
        _settingsMenu.DataContext = _settingsVM;
        
        //Commands
        
        LaunchCommand = new RelayCommand(async o => await HandlePlayButtonPress());
        
        NavigateCommand = new RelayCommand(parameter => 
        {
            if (parameter is string pageName)
            {
                switch (pageName)
                {
                    case "Play": CurrentView = PlayVM; break;
                    case "Installations": CurrentView = InstallationsVM; break;
                    case "Skins": CurrentView = SkinsVM; break;
                }
            }
        });
        CurrentView = PlayVM;
        
        OpenSettingsCommand = new RelayCommand(o => _appStore.CurrentOverlayView = _settingsMenu);
        
        OpenLoginCommand = new RelayCommand(o => 
        {
           
            _loginVM.IsAddAccPageOpen = !_loginStore.IsLoggedIn; 
            var loginMenu = new LoginMenu();
            loginMenu.DataContext = _loginVM; 
            _appStore.CurrentOverlayView = loginMenu;
        });

        CloseOverlayCommand = new RelayCommand(o => _appStore.CurrentOverlayView = null);
        
        DragEnterCommand = new RelayCommand(o => HandleDragEnter(o));
        DragLeaveCommand = new RelayCommand(o => HandleDragLeave(o));
        DropCommand = new RelayCommand(async o => await HandleDropAsync(o));
        
        _loginVM.RequestClose += () =>
        {
            _appStore.CurrentOverlayView = null;
        };
        _settingsVM.RequestClose += () => _appStore.CurrentOverlayView = null;
        _playVM.RequestLaunch += async () => await HandlePlayButtonPress();
        _discordService.Initialize(Core.Constants.DiscordAppId);
    }
    
    public async Task RefreshAllAccountsAsync()
    {
        bool isChanged = false;
            
        var accountsList = _loginStore.Accounts.ToList(); 

        foreach (var acc in accountsList)
        {
            if (acc.IsOffline) continue;

            try
            {
                await _authService.ValidateAndRefreshAccountAsync(acc);
                isChanged = true; 
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Auth] Account {acc.Username} validation failed: {ex.Message}");
                    
                _loginStore.Accounts.Remove(acc);
                isChanged = true;
                    
                if (_loginStore.CurrentAccount == acc && _loginStore.Accounts.Count > 0)
                {
                    _loginStore.CurrentAccount = _loginStore.Accounts.FirstOrDefault();
                }
                else if (_loginStore.Accounts.Count == 0)
                {
                    _loginStore.CurrentAccount = null;
                }
            }
        }
        if (isChanged)
        {
            _accountStorage.SaveAccounts(_loginStore.Accounts);
        }
    }
    
    private async Task HandlePlayButtonPress()
    {
        // Если игра уже запущена - закрываем её
        if (_appStore.IsGameRunning)
        {
            await CloseGameProcess();
        }
        // Если идет загрузка - просто игнорируем нажатие (так как отмену мы убрали)
        else if (_appStore.IsDownloading)
        {
            return; 
        }
        // Иначе - запускаем
        else
        {
            await LaunchCurrentInstance(); 
        }
    }
    
    private async Task CloseGameProcess()
    {
        if (_currentGameProcess != null && !_currentGameProcess.HasExited)
        {
            try
            {
                _currentGameProcess.Kill(); // Заменили Close() на Kill()
                await _currentGameProcess.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing game process: {ex}");
            }
        }
    }
    
    private async Task LaunchCurrentInstance()
    {
        if (_appStore.IsDownloading) return;
        if (_appStore.IsGameRunning) return;

        if (_instancesStore.SelectedInstance == null) return;
        if (_loginStore.CurrentAccount == null) 
        { 
            OpenLoginCommand.Execute(null); 
            return;
        }

        try
        {
            IsEnabledInstancesComboBox = false;
            _appStore.IsDownloading = true;
            OnPropertyChanged(nameof(_appStore.DownloadPanelVisibility));
            _playVM.DownloadStatusText = "Preparing...";
            _playVM.DownloadProgress = 0;

            // Полностью перешли на LaunchState
            var progress = new Progress<LaunchState>(state =>
            {
                _playVM.DownloadProgress = state.Progress;
                _playVM.DownloadStatusText = state.StatusText; 
            });
        
            var globalSettings = new GlobalLaunchSettings
            {
                MaxRamMb = _settingsStore.SelectedMaxRam,
                IsFullscreen = _settingsStore.IsGameFullScreen,
                Resolution = _settingsStore.IsGameFullScreen ? "Auto" : _settingsStore.SelectedResolution
            };

            _currentGameProcess = await _launchService.LaunchGameAsync(_instancesStore.SelectedInstance,
                _loginStore.CurrentAccount, globalSettings, progress);

            Console.WriteLine("Game started!");

            // === ИГРА ЗАПУЩЕНА ===
            _appStore.IsGameRunning = true;
            _playVM.UpdateDiscordPresence();
            _appStore.IsDownloading = false;
            _instancesStore.ApplySort();
        
            // hide main window when game is launched
            if (!_settingsStore.IsKeepLauncherOpen) Application.Current.MainWindow.Hide();
            
            // Меняем текст кнопки на "Close"
            _playVM.CurrentPlayButtonText.Update("Play.PlayButton.Close"); 
            _playVM.ChangeToPlayIcon("Icon.Close"); // Устанавливаем иконку крестика (предварительно добавив её в ресурсы)
        
            OnPropertyChanged(nameof(_appStore.DownloadPanelVisibility)); 
            _instanceService.SaveInstances(_instancesStore.Instances);

            await _currentGameProcess.WaitForExitAsync();
            
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            OnPropertyChanged(nameof(_appStore.DownloadPanelVisibility)); 
        }
        finally
        {
            // === СБРОС СОСТОЯНИЯ (игра закрыта сама или убита кнопкой) ===
            _appStore.IsDownloading = false;
            _appStore.IsGameRunning = false;
            IsEnabledInstancesComboBox = true;
            Application.Current.MainWindow.Show();
            
            // Возвращаем текст кнопки на "PLAY"
            _playVM.CurrentPlayButtonText.Update("Play.PlayButton");
            _playVM.UpdateDiscordPresence();
            _playVM.ChangeToPlayIcon("Icon.Play"); // И возвращаем иконку "Play"
        
            _currentGameProcess = null;
        }
    }

    private void HandleDragEnter(object parameter)
    {
        if (_appStore.CurrentOverlayView != null) return; 
        if (parameter is DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                _appStore.IsDragDropActive = true;
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
    }

    private void HandleDragLeave(object parameter)
    {
        _appStore.IsDragDropActive = false;
    }

    private async Task HandleDropAsync(object parameter)
    {
        _appStore.IsDragDropActive = false;
        
        if (parameter is DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    using var cts = new CancellationTokenSource();
                    
                    var progressVM = new ProgressVM(
                        onHide: () => _appStore.IsOverlayVisible = false, 
                        onCancel: () => cts.Cancel()                      
                    )
                    {
                        Title = LocalizationService.Instance["ProgressMenu.ImportingFiles.Title"],
                        Message = String.Format(LocalizationService.Instance["ProgressMenu.ImportingFiles.DescriptionPreparing"], files.Count()),
                        IsIndeterminate = false,
                        ProgressValue = 0,
                        ProgressText = "0%"
                    };

                    var progressView = new ProgressMenu { DataContext = progressVM };
                    _appStore.CurrentOverlayView = progressView;
                    
                    
                    int totalFiles = files.Length;
                    int processedFiles = 0;
                    int successCount = 0;
                    
                    bool isThemeLoaded = false;
                    bool isLocalizationLoaded = false;
                    var currentInstance = _instancesStore.SelectedInstance; 

                    try
                    {
                        foreach (var file in files)
                        {
                            cts.Token.ThrowIfCancellationRequested();

                            string fileName = Path.GetFileName(file);
                            
                            fileName = fileName.Length > 32 ? fileName.Remove(32) : fileName;
                            progressVM.Message = String.Format(LocalizationService.Instance["ProgressMenu.ImportingFiles.Description"], fileName);

                            try
                            {
                                var fileType = await _dragDropParserService.ParseFileAsync(file);
                                
                                switch (fileType)
                                {
                                    case ParsedFileType.MinecraftMod:
                                        if (currentInstance == null) throw new InvalidOperationException("Select an instance to install the mod.");
                                        await _instanceFileSystemService.ImportModAsync(currentInstance, file);
                                        break;
                                    
                                    case ParsedFileType.MinecraftResourcepack:
                                        if (currentInstance == null) throw new InvalidOperationException("Select an instance to install the resource pack.");
                                        await _instanceFileSystemService.ImportResourcePackAsync(currentInstance, file);
                                        break;
                                        
                                    case ParsedFileType.MinecraftShaderpack:
                                        if (currentInstance == null) throw new InvalidOperationException("Select an instance to install the shader pack.");
                                        await _instanceFileSystemService.ImportShaderPackAsync(currentInstance, file);
                                        break;
                                        
                                    case ParsedFileType.MinecraftWorldSave:
                                        if (currentInstance == null) throw new InvalidOperationException("Select an instance to import the world save.");
                                        await _instanceFileSystemService.ImportSaveAsync(currentInstance, file);
                                        break;
                                        
                                    case ParsedFileType.LauncherTheme:
                                        await _themeService.ImportTheme(file);
                                        isThemeLoaded = true;
                                        break;
                                        
                                    case ParsedFileType.LauncherLocalization:
                                        await LocalizationService.Instance.ImportLocalization(file);
                                        isLocalizationLoaded = true;
                                        break;
                                        
                                    case ParsedFileType.Unknown:
                                    default:
                                        continue;
                                }
                                
                                successCount++;
                            }
                            catch (OperationCanceledException)
                            {
                                throw; 
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error importing file '{file}': {ex}");
                            }
                            finally
                            {
                                processedFiles++;
                                double currentProgress = ((double)processedFiles / totalFiles) * 100;
                                progressVM.Report(currentProgress);
                            }
                        }

                        if (isThemeLoaded) 
                        {
                            var lastThemePath = _settingsStore.CurrentThemePath;
                            _settingsStore.AvailableThemes.Clear();
                            var themes = _themeService.ReloadThemes();
                            foreach (var theme in themes) _settingsStore.AvailableThemes.Add(theme);
                            _settingsStore.CurrentThemePath = lastThemePath;
                        }

                        if (isLocalizationLoaded)
                        {
                            var lastLangCode = _settingsStore.SelectedLanguage.Code;
                            
                            _settingsStore.AvailableLanguages.Clear(); 
                            
                            var langs = LocalizationService.Instance.GetAvailableLanguages();
                            foreach (var lang in langs) _settingsStore.AvailableLanguages.Add(lang);
                            _settingsStore.SelectedLanguage = _settingsStore.AvailableLanguages.FirstOrDefault(l => l.Code == lastLangCode);
                        }

                        if (successCount > 0)
                        {
                            var title = LocalizationService.Instance["Success.SuccessImport"];
                            var desc = String.Format(LocalizationService.Instance["Success.SuccessImportDesc"], successCount, totalFiles);
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
                        _appStore.CurrentOverlayView = null;
                    }
                }
            }
            e.Handled = true;
        }
    }

    //Getters & Setters

    public bool IsEnabledInstancesComboBox
    {
        get => _isEnabledInstancesComboBox;
        set
        {
            if (_isEnabledInstancesComboBox != value)
            {
                _isEnabledInstancesComboBox = value;
                OnPropertyChanged(nameof(IsEnabledInstancesComboBox));
            }
        }
    }
    
    public object CurrentView
    {
        get => _currentView;
        set
        {
            if (_currentView != value)
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
                
                ShowCompactPlayButton = !(_currentView is PlayViewModel);
            }
        }
    }

    public bool ShowCompactPlayButton
    {
        get => _showCompactPlayButton;
        set
        {
            if (_showCompactPlayButton != value)
            {
                _showCompactPlayButton = value;
                OnPropertyChanged(nameof(ShowCompactPlayButton));
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}