using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Launcher.Core.Models;
using System.Threading.Tasks; 
using System.Windows;   
using System.Windows.Input;
using Launcher.Core.Services.Auth; 
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ThemeService _themeService;
    private readonly IAuthService _authService; 

    public InstallationsViewModel InstallationsVM { get; }


    private double _uiScale = 1.0;
    private bool _isOverlayVisible;
    private object _currentOverlayView;
    private string _currentThemePath = "/Assets/Themes/default-dark.xaml";


    private bool _isLoggingIn;
    private bool _isAddAccPageOpen;
    private string _userName = "Guest";
    private string _userUuid;
    
    public bool IsLoggedIn => CurrentAccount != null;
    
    private UserAccount _currentAccount;
    public ObservableCollection<UserAccount> Accounts { get; set; } = new();
    
    public UserAccount CurrentAccount
    {
        get => _currentAccount;
        set
        {
            _currentAccount = value;
            OnPropertyChanged(nameof(CurrentAccount));
            OnPropertyChanged(nameof(IsLoggedIn)); 
        }
    }

    public bool IsAddAccPageOpen
    {
        get => _isAddAccPageOpen;
        set
        {
            _isAddAccPageOpen = value;
            OnPropertyChanged(nameof(IsAddAccPageOpen));
        }
    }
    
    public bool IsLoggingIn
    {
        get => _isLoggingIn;
        set
        {
            _isLoggingIn = value;
            OnPropertyChanged(nameof(IsLoggingIn));
        }
    }

    public string UserName
    {
        get => _userName;
        set
        {
            _userName = value;
            OnPropertyChanged(nameof(UserName));
        }
    }
    
    public ICommand ChangeThemeCommand { get; }
    public ICommand CloseOverlayCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenAddVersionCommand { get; }
    public ICommand OpenLoginCommand { get; }
    
    public ICommand MicrosoftLoginCommand { get; } 
    public ICommand OfflineLoginCommand { get; }
    public ICommand AddNewAccountCommand { get; }


    public MainViewModel(ThemeService themeService, IAuthService authService)
    {
        _themeService = themeService;
        _authService = authService;
        
        InstallationsVM = new InstallationsViewModel(this);
        
        MicrosoftLoginCommand = new RelayCommand(async (o) => await ExecuteLogin());
        OfflineLoginCommand = new RelayCommand(o => LoginOffline(o as string));
        AddNewAccountCommand = new RelayCommand(o =>
        {
            IsAddAccPageOpen = true;
        }); 

        OpenSettingsCommand = new RelayCommand(o => 
        {
            CurrentOverlayView = new Resources.Overlay.SettingsMenu();
        });
        
        OpenLoginCommand = new RelayCommand(o => 
        {
            if (IsLoggedIn) IsAddAccPageOpen = false; else IsAddAccPageOpen = true;
            var loginMenu = new Resources.Overlay.LoginMenu();
            loginMenu.DataContext = this; 
            
            CurrentOverlayView = loginMenu;
        });
        
        OpenAddVersionCommand = new RelayCommand(o => 
        {
            InstallationsVM.OpenAddVersionCommand.Execute(o);
        });
        
        _themeService.ChangeTheme(_currentThemePath);
        
        ChangeThemeCommand = new RelayCommand(path => 
        {
            if (path is string themePath)
            {
                _themeService.ChangeTheme(themePath);
            }
        });

        CloseOverlayCommand = new RelayCommand(o => 
        {
            CurrentOverlayView = null;
        });
    }
    
    private async Task ExecuteLogin()
    {
        IsLoggingIn = true;
        try
        {
            var session = await _authService.LoginWithMicrosoftAsync();
            
            var newAccount = new UserAccount(
                session.Username, 
                session.UUID, 
                session.AccessToken, 
                isOffline: false);
            
            var existing = Accounts.FirstOrDefault(x => x.UUID == newAccount.UUID);
            if (existing == null)
            {
                Accounts.Add(newAccount);
                CurrentAccount = newAccount;
            }
            else
            {
                CurrentAccount = existing;
            }

            CurrentOverlayView = null;
        }
        finally { IsLoggingIn = false; }
    }

    public void LoginOffline(string nickname)
    {
        string fakeUuid = Guid.NewGuid().ToString(); 
    
        var newAccount = new UserAccount(
            nickname, 
            fakeUuid, 
            token: string.Empty, 
            isOffline: true);
        Accounts.Add(newAccount);
        
        CurrentAccount = newAccount;
        
        CurrentOverlayView = null;
    }
    
    
    

    public double UiScale
    {
        get => _uiScale;
        set
        {
            if (Math.Abs(_uiScale - value) > 0.001)
            {
                _uiScale = value;
                OnPropertyChanged(nameof(UiScale));
            }
        }
    }

    public string CurrentThemePath
    {
        get => _currentThemePath;
        set
        {
            if (_currentThemePath != value)
            {
                _currentThemePath = value;
                OnPropertyChanged(nameof(CurrentThemePath));
                _themeService.ChangeTheme(_currentThemePath);
            }
        }
    }

    public bool IsOverlayVisible
    {
        get => _isOverlayVisible;
        set
        {
            _isOverlayVisible = value;
            OnPropertyChanged(nameof(IsOverlayVisible));
        }
    }

    public object CurrentOverlayView
    {
        get => _currentOverlayView;
        set
        {
            _currentOverlayView = value;
            OnPropertyChanged(nameof(CurrentOverlayView));
            IsOverlayVisible = _currentOverlayView != null;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}