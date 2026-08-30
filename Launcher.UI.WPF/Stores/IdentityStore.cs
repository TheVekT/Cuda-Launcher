using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Identity.Abstractions;
using Launcher.Core.Identity.Exceptions;
using Launcher.Core.Identity.Models;
using Launcher.Infrastructure.Config.Abstractions;
using Launcher.Infrastructure.Config.Models;
using Launcher.UI.WPF.Helpers.Collections;
using Launcher.UI.WPF.Helpers.Localization;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Models.Shell;
using Launcher.UI.WPF.Services.Shell.Abstractions;
using Launcher.UI.WPF.Services.Windows.Abstractions;

namespace Launcher.UI.WPF.Stores;

public partial class IdentityStore : ObservableObject
{
    private readonly IAccountStorageService _accountStorage;
    private readonly IAuthService _authService;
    private readonly IDispatcherService _dispatcherService;
    private readonly INotificationService _notificationService;
    
    private UserAccount? _currentAccount;
    [ObservableProperty]
    private string _userName = "Guest";
    [ObservableProperty]
    private bool _isLoggingIn;
    [ObservableProperty]
    [property: SettingProperty]
    private string? _lastSelectedAccountUuid;
    
    public bool IsLoggedIn => CurrentAccount != null;
    public ObservableRangeCollection<UserAccount> Accounts { get; set; } = new();

    // Добавили ISettingsService в конструктор
    public IdentityStore(IAccountStorageService accountStorage, 
        ISettingsService settingsService, 
        IAuthService authService,
        IDispatcherService dispatcherService,
        INotificationService notificationService)
    {
        _accountStorage = accountStorage;
        _authService = authService;
        _dispatcherService = dispatcherService;
        _notificationService = notificationService;
        
        settingsService.Initialize(this);
        
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
        var accountsList = Accounts.ToList(); 

        foreach (var acc in accountsList)
        {
            if (acc.IsOffline) continue;

            try
            {
                await _authService.ValidateAndRefreshAccountAsync(acc);
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"[Auth] Account {acc.Username} is unauthorized or token expired: {ex.Message}");
                
                await _dispatcherService.InvokeAsync(() => 
                {
                    Accounts.Remove(acc);
                    
                    if (CurrentAccount == acc && Accounts.Count > 0)
                        CurrentAccount = Accounts.FirstOrDefault();
                    else if (Accounts.Count == 0)
                        CurrentAccount = null;
                });
                var title = LocalizableText.Key(LocKey.Info_MojangTokenExpired_Title);
                var description = LocalizableText.Key(LocKey.Info_MojangTokenExpired_Desc, acc.Username);
                _notificationService.ShowInfo(title, description);
            }
            catch (MinecraftNotPurchasedException ex)
            {
                Debug.WriteLine($"[Auth] Account {acc.Username} has no Minecraft license: {ex.Message}");
                
                await _dispatcherService.InvokeAsync(() => 
                {
                    Accounts.Remove(acc);
                    
                    if (CurrentAccount == acc && Accounts.Count > 0)
                        CurrentAccount = Accounts.FirstOrDefault();
                    else if (Accounts.Count == 0)
                        CurrentAccount = null;
                });
                var title = LocalizableText.Key(LocKey.Errors_NoMinecraftLicense_Title);
                var description = LocalizableText.Key(LocKey.Errors_NoMinecraftLicense_Desc);
                _notificationService.ShowError(title, description);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Auth] Unexpected error while refreshing account {acc.Username}: {ex.Message}");
            }
        }
        _accountStorage.SaveAccounts(Accounts);
    }
    
    //Getters and Setters
    
    public UserAccount? CurrentAccount
    {
        get => _currentAccount;
        set
        {
            if (_currentAccount != value)
            {
                _currentAccount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLoggedIn)); 
                if (value != null)
                {
                    WeakReferenceMessenger.Default.Send(new AccountLoggedMessage(value));
                    LastSelectedAccountUuid = _currentAccount?.UUID;
                }
            }
        }
    }
}
