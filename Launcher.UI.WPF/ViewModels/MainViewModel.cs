using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq; 
using System.Threading.Tasks; 
using System.Windows;   
using System.Windows.Input;
using Launcher.Core.Models; // Твоя модель MinecraftInstance
using Launcher.Core.Services.Auth; 
using Launcher.Core.Services.IO; // Твой InstanceService
using Launcher.Core.Services.Game;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ThemeService _themeService;
    private readonly IAuthService _authService; 
    private readonly IAccountStorageService _accountStorage;
    private readonly IInstanceService _instanceService; // <-- Сервис инстансов

    public InstallationsViewModel InstallationsVM { get; }

    // --- Свойства UI ---
    private double _uiScale = 1.0;
    private bool _isOverlayVisible;
    private object _currentOverlayView;
    private string _currentThemePath = "/Assets/Themes/default-dark.xaml";

    // --- Свойства Аккаунта ---
    private bool _isLoggingIn;
    private bool _isAddAccPageOpen;
    private string _userName = "Guest";
    private UserAccount _currentAccount;
    
    public bool IsLoggedIn => CurrentAccount != null;
    public ObservableCollection<UserAccount> Accounts { get; set; } = new();

    // --- Свойства Инстансов (ЭТАП 1) ---
    public ObservableCollection<MinecraftInstance> Instances { get; set; } = new();

    private MinecraftInstance _selectedInstance;
    public MinecraftInstance SelectedInstance
    {
        get => _selectedInstance;
        set
        {
            if (_selectedInstance != value)
            {
                _selectedInstance = value;
                OnPropertyChanged(nameof(SelectedInstance));
            }
        }
    }

    // --- Команды ---
    public ICommand ChangeThemeCommand { get; }
    public ICommand CloseOverlayCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand OpenAddVersionCommand { get; }
    public ICommand OpenLoginCommand { get; }
    public ICommand SelectAccountCommand { get; }
    public ICommand MicrosoftLoginCommand { get; } 
    public ICommand OfflineLoginCommand { get; }
    public ICommand AddNewAccountCommand { get; }

    public async Task InitializeAsync()
    {
        await InstallationsVM.InitializeAsync();
    }

    public MainViewModel(
        ThemeService themeService, 
        IAuthService authService, 
        IAccountStorageService accountStorage,
        IGameVersionService versionService,
        InstanceService instanceService) // Inject InstanceService
    {
        _themeService = themeService;
        _authService = authService;
        _accountStorage = accountStorage;
        _instanceService = instanceService;
        
        // 1. Загрузка аккаунтов
        LoadSavedAccounts();
        
        // 2. Загрузка инстансов (ЭТАП 1)
        LoadSavedInstances();

        // Передаем this (MainViewModel), чтобы InstallationsVM мог добавлять инстансы в наш список
        InstallationsVM = new InstallationsViewModel(this, versionService, instanceService);
        
        // --- Инициализация команд ---
        MicrosoftLoginCommand = new RelayCommand(async (o) => await ExecuteMicrosoftLogin());
        
        OfflineLoginCommand = new RelayCommand(o => 
        {
            if (o is string nickname && !string.IsNullOrWhiteSpace(nickname))
            {
                var account = _authService.LoginOffline(nickname);
                ProcessSuccessfulLogin(account);
            }
        });
        
        SelectAccountCommand = new RelayCommand(o => 
        {
            if (o is UserAccount account)
            {
                CurrentAccount = account;
                _accountStorage.SaveAccounts(Accounts); 
            }
        });

        AddNewAccountCommand = new RelayCommand(o => IsAddAccPageOpen = true); 

        OpenSettingsCommand = new RelayCommand(o => CurrentOverlayView = new Resources.Overlay.SettingsMenu());
        
        OpenLoginCommand = new RelayCommand(o => 
        {
            IsAddAccPageOpen = !IsLoggedIn; 
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
            if (path is string themePath) _themeService.ChangeTheme(themePath);
        });

        CloseOverlayCommand = new RelayCommand(o => CurrentOverlayView = null);
    }
    
    // --- Логика Инстансов ---
    private void LoadSavedInstances()
    {
        var loaded = _instanceService.LoadInstances();
        Instances.Clear();
        foreach (var inst in loaded)
        {
            Instances.Add(inst);
        }
        
        // Выбираем первый по умолчанию
        if (Instances.Count > 0) SelectedInstance = Instances[0];
    }

    // --- Логика Аккаунтов ---
    private void LoadSavedAccounts()
    {
        var savedAccounts = _accountStorage.LoadAccounts();
        Accounts.Clear();
        foreach (var acc in savedAccounts) Accounts.Add(acc);
        
        var lastUsedAccount = Accounts.FirstOrDefault(x => x.IsSelected);
        if (lastUsedAccount != null) CurrentAccount = lastUsedAccount;
        else if (Accounts.Count > 0) CurrentAccount = Accounts.First();
    }
    
    public UserAccount CurrentAccount
    {
        get => _currentAccount;
        set
        {
            if (_currentAccount != value)
            {
                _currentAccount = value;
                OnPropertyChanged(nameof(CurrentAccount));
                OnPropertyChanged(nameof(IsLoggedIn)); 
                if (_currentAccount != null)
                {
                    UserName = _currentAccount.Username;
                    foreach (var acc in Accounts) acc.IsSelected = (acc.UUID == _currentAccount.UUID);
                    _accountStorage.SaveAccounts(Accounts);
                }
            }
        }
    }

    // ... (Методы входа Microsoft/Offline остались без изменений) ...
    private async Task ExecuteMicrosoftLogin()
    {
        IsLoggingIn = true;
        try
        {
            var newAccount = await _authService.LoginWithMicrosoftAsync();
            ProcessSuccessfulLogin(newAccount);
        }
        catch (Exception ex) { Console.WriteLine(ex.Message); }
        finally { IsLoggingIn = false; }
    }

    private void ProcessSuccessfulLogin(UserAccount newAccount)
    {
        var existing = Accounts.FirstOrDefault(x => x.UUID == newAccount.UUID);
        if (existing == null)
        {
            Accounts.Add(newAccount);
            CurrentAccount = newAccount;
        }
        else
        {
            existing.AccessToken = newAccount.AccessToken; 
            CurrentAccount = existing;
        }
        _accountStorage.SaveAccounts(Accounts);
        CurrentOverlayView = null;
    }

    // ... (Геттеры и Сеттеры IsAddAccPageOpen, IsLoggingIn, UserName, UiScale и др. без изменений) ...

    public bool IsAddAccPageOpen
    {
        get => _isAddAccPageOpen;
        set { _isAddAccPageOpen = value; OnPropertyChanged(nameof(IsAddAccPageOpen)); }
    }
    public bool IsLoggingIn
    {
        get => _isLoggingIn;
        set { _isLoggingIn = value; OnPropertyChanged(nameof(IsLoggingIn)); }
    }
    public string UserName
    {
        get => _userName;
        set { _userName = value; OnPropertyChanged(nameof(UserName)); }
    }
    public double UiScale
    {
        get => _uiScale;
        set { if (Math.Abs(_uiScale - value) > 0.001) { _uiScale = value; OnPropertyChanged(nameof(UiScale)); } }
    }
    public string CurrentThemePath
    {
        get => _currentThemePath;
        set { if (_currentThemePath != value) { _currentThemePath = value; OnPropertyChanged(nameof(CurrentThemePath)); _themeService.ChangeTheme(_currentThemePath); } }
    }
    public bool IsOverlayVisible
    {
        get => _isOverlayVisible;
        set { _isOverlayVisible = value; OnPropertyChanged(nameof(IsOverlayVisible)); }
    }
    public object CurrentOverlayView
    {
        get => _currentOverlayView;
        set { _currentOverlayView = value; OnPropertyChanged(nameof(CurrentOverlayView)); IsOverlayVisible = _currentOverlayView != null; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}