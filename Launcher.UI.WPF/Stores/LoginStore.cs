using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.Core.Models;
using Launcher.Core.Services.Auth;
using Launcher.Core.Services.IO;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Services;

namespace Launcher.UI.WPF.Stores;

public partial class LoginStore : ObservableObject
{
    private readonly IAccountStorageService _accountStorage;
    private readonly ISettingsService _settingsService;
    private readonly IAuthService _authService;
    private readonly IDispatcherService _dispatcherService;
    
    private bool _isLoggingIn;
    private UserAccount _currentAccount;
    private string _userName = "Guest";
    [ObservableProperty]
    [property: SettingProperty]
    private string _lastSelectedAccountUuid; // ID для сохранения в settings.json
    
    public bool IsLoggedIn => CurrentAccount != null;
    public ObservableRangeCollection<UserAccount> Accounts { get; set; } = new();

    // Добавили ISettingsService в конструктор
    public LoginStore(IAccountStorageService accountStorage, 
        ISettingsService settingsService, 
        IAuthService authService,
        IDispatcherService dispatcherService)
    {
        _accountStorage = accountStorage;
        _settingsService = settingsService;
        _authService = authService;
        _dispatcherService = dispatcherService;
        
        _settingsService.Initialize(this);
        
        LoadSavedAccounts();
    }



    public void RegisterLogin(UserAccount newAccount)
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
            existing.Username = newAccount.Username;
            CurrentAccount = existing;
        }
        
        _accountStorage.SaveAccounts(Accounts);
    }
    
    private void LoadSavedAccounts()
    {
        var savedAccounts = _accountStorage.LoadAccounts();
        Accounts.ReplaceRange(savedAccounts);
    
        // Ищем по UUID, который загрузился из settings.json
        if (!string.IsNullOrEmpty(LastSelectedAccountUuid))
        {
            var lastUsedAccount = Accounts.FirstOrDefault(x => x.UUID == LastSelectedAccountUuid);
            if (lastUsedAccount != null)
            {
                CurrentAccount = lastUsedAccount;
                return;
            }
        }
        
        if (Accounts.Count > 0) CurrentAccount = Accounts.First();
    }
    
    public async Task RefreshAllAccountsAsync()
    {
        bool isChanged = false;
        
        var accountsList = Accounts.ToList(); 

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
                
                _dispatcherService.Invoke(() => 
                {
                    Accounts.Remove(acc);
    
                    isChanged = true;
    
                    if (CurrentAccount == acc && Accounts.Count > 0)
                    {
                        CurrentAccount = Accounts.FirstOrDefault();
                    }
                    else if (Accounts.Count == 0)
                    {
                        CurrentAccount = null;
                    }
                });
            }
        }
        if (isChanged)
        {
            _accountStorage.SaveAccounts(Accounts);
        }
    }
    
    //Getters and Setters
    
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
                    LastSelectedAccountUuid = _currentAccount.UUID;
                }
            }
        }
    }
}
