using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Launcher.Core.Models;
using Launcher.Core.Services.Auth; 
using Launcher.Core.Services.IO;
using Launcher.Core.Services.Game;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Resources.Overlay;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;

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
    
    //Overlays
    private SettingsMenu _settingsMenu;
    
    //Commands
    public ICommand LaunchCommand { get; }
    public ICommand CloseOverlayCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenLoginCommand { get; }
    public ICommand NavigateCommand { get; }
    
    //public attributes
    public AppStore AppStore => _appStore;

    public async Task InitializeAsync()
    {
        _ = Task.Run(async () => await _versionService.GetGameVersionsByTypeAsync(GameLoaderType.Vanilla));
        // async initialization logic here (e.g. load accounts, instances, etc.)
    }

    public MainViewModel(
        ThemeService themeService, 
        IAuthService authService, 
        IAccountStorageService accountStorage,
        IGameVersionService versionService,
        InstanceService instanceService,
        IInstanceFileSystemService instanceFileSystemService,
        ILaunchService launchService,
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
        
        //Stores
        _loginStore = loginStore;
        _settingsStore = settingsStore;
        _launchStore = launchStore;
        _instancesStore = instancesStore;
        _appStore = appStore;
        
        //ViewModels
        _playVM = new PlayViewModel();
        _installationsVM = new InstallationsViewModel(versionService, instanceService, instanceFileSystemService, instancesStore, appStore);
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
        
        _loginVM.RequestClose += () =>
        {
            _appStore.CurrentOverlayView = null;
        };
        _settingsVM.RequestClose += () => _appStore.CurrentOverlayView = null;
        _playVM.RequestLaunch += async () => await HandlePlayButtonPress();
        
    }
    
    private async Task HandlePlayButtonPress()
    {
        // Если игра уже запущена - закрываем её
        if (_playVM.IsGameRunning)
        {
            await CloseGameProcess();
        }
        // Если идет загрузка - просто игнорируем нажатие (так как отмену мы убрали)
        else if (_playVM.IsDownloading)
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
        if (_playVM.IsDownloading) return;
        if (_playVM.IsGameRunning) return;
    
        Console.WriteLine("Launching instance...");
        if (_instancesStore.SelectedInstance == null) return;
        if (_loginStore.CurrentAccount == null) 
        { 
            OpenLoginCommand.Execute(null); 
            return;
        }

        try
        {
            _playVM.IsDownloading = true;
            OnPropertyChanged(nameof(_playVM.DownloadPanelVisibility));
            _playVM.DownloadStatusText = "Preparing...";
            _playVM.DownloadProgress = 0;

            var progress = new Progress<double>(p =>
            {
                _playVM.DownloadProgress = p;
                if (p < 100) _playVM.DownloadStatusText = "Downloading files...";
                else _playVM.DownloadStatusText = "Finalizing..."; 
            });

            _currentGameProcess = await _launchService.LaunchGameAsync(_instancesStore.SelectedInstance,
                _loginStore.CurrentAccount, progress);

            Console.WriteLine("Game started!");

            // === ИГРА ЗАПУЩЕНА ===
            _playVM.IsGameRunning = true;
            _playVM.IsDownloading = false;
        
            // Меняем текст кнопки на "Close"
            _playVM.CurrentPlayButtonText.Update("Play.PlayButton.Close"); 
        
            OnPropertyChanged(nameof(_playVM.DownloadPanelVisibility)); 
            _instanceService.SaveInstances(_instancesStore.Instances);

            await _currentGameProcess.WaitForExitAsync(); 
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            OnPropertyChanged(nameof(_playVM.DownloadPanelVisibility)); 
        }
        finally
        {
            // === СБРОС СОСТОЯНИЯ (игра закрыта сама или убита кнопкой) ===
            _playVM.IsDownloading = false;
            _playVM.IsGameRunning = false;
        
            // Возвращаем текст кнопки на "PLAY"
            _playVM.CurrentPlayButtonText.Update("Play.PlayButton");
        
            _currentGameProcess = null;
        }
    }

    //Getters & Setters
    
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