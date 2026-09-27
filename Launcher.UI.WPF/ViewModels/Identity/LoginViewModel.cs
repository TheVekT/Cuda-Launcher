using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Identity.Abstractions;
using Launcher.Core.Identity.Exceptions;
using Launcher.Core.Identity.Models;
using Launcher.UI.WPF.Helpers.Localization;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Models.Shell;
using Launcher.UI.WPF.Services.Shell.Abstractions;
using Launcher.UI.WPF.Services.Windows.Abstractions;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels.Identity;
public partial class LoginViewModel: ObservableObject
{ 
    //Services
    private readonly IAuthService _authService;
    private readonly IAccountStorageService _accountStorage;
    private readonly IOverlayService _overlayService;
    private readonly INotificationService _notificationService;
    private readonly IBrowserService _browserService;
    //Stores
    private readonly IdentityStore _identityStore;
    //Attributes
    public IdentityStore IdentityStore => _identityStore;
    
    [ObservableProperty]
    private bool _isAddAccPageOpen;
    [ObservableProperty]
    private bool _isLoggingIn;
    [ObservableProperty]
    private int _accountCount;

    public LoginViewModel(IAuthService authService, 
        IAccountStorageService accountStorage,
        IOverlayService overlayService,
        INotificationService notificationService,
        IBrowserService browserService,
        IdentityStore identityStore)
    { 
        _authService = authService; 
        _accountStorage = accountStorage;
        _overlayService = overlayService;
        _notificationService = notificationService;
        _identityStore = identityStore;
        _browserService = browserService;
            
        AccountCount = _identityStore.Accounts.Count;
        
            
        _identityStore.Accounts.CollectionChanged += (_, _) => 
        {
            AccountCount = _identityStore.Accounts.Count;
        };
    }
        
    private void HandleRenameAccount(UserAccount account)
    {
        if (!account.IsOffline)
        {
            //Open URL https://www.minecraft.net/en-us/msaprofile/mygames/editprofile
            _browserService.OpenUrl("https://www.minecraft.net/en-us/msaprofile/mygames/editprofile");
        }
    }
        
    private void HandleDeleteAccount(UserAccount account)
    {
        Console.WriteLine($"Deleting account {account.Username}");
        var isSelectedAccountToDelete = _identityStore.CurrentAccount == account;
        _identityStore.Accounts.Remove(account);
        _accountStorage.SaveAccounts(_identityStore.Accounts);
            
        if (isSelectedAccountToDelete && _identityStore.Accounts.Count > 0)
        {
            _identityStore.CurrentAccount = _identityStore.Accounts.FirstOrDefault();
        }
        else if (_identityStore.Accounts.Count > 0)
        {
            _identityStore.CurrentAccount = _identityStore.CurrentAccount; 
        }
        else
        {
            IsAddAccPageOpen = true;
            _identityStore.CurrentAccount = null;
        }
    }
        

    private void ExecuteOffileLogin(object o)
    {
        if (o is string nickname && !string.IsNullOrWhiteSpace(nickname))
        {
            var account = _authService.LoginOffline(nickname);
                
            _identityStore.RegisterLogin(account); 
                
            WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
            WeakReferenceMessenger.Default.Send(new AccountLoggedMessage(account));
        }
    }


    private async Task ExecuteMicrosoftLogin()
    {
        if (IsLoggingIn) return;

        IsLoggingIn = true;
        try
        {
            _overlayService.SetClosable(false);
            var newAccount = await _authService.LoginWithMicrosoftAsync();
                
            _identityStore.RegisterLogin(newAccount);
            var title = LocalizableText.Key(LocKey.Success_LoginMicrosoftTitle);
            var desc = LocalizableText.Key(LocKey.Success_LoginMicrosoftDesc);
            _notificationService.ShowSuccess(title, desc);
            _overlayService.SetClosable(true);
            WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
            WeakReferenceMessenger.Default.Send(new AccountLoggedMessage(newAccount));
        }
        catch (MinecraftNotPurchasedException ex)
        {
            Debug.WriteLine($"[Auth Error] Minecraft license not found: {ex.Message}");
            var title = LocalizableText.Key(LocKey.Errors_NoMinecraftLicense_Title);
            var desc = LocalizableText.Key(LocKey.Errors_NoMinecraftLicense_Desc);
            _notificationService.ShowError(title, desc);
        }
        catch (Exception ex) 
        { 
            Debug.WriteLine($"[Auth Error] Microsoft login failed: {ex}");
            var title = LocalizableText.Key(LocKey.Errors_LoginMicrosoftTitle);
            var desc = LocalizableText.Key(LocKey.Errors_LoginMicrosoftDesc);
            _notificationService.ShowError(title, desc);
        }
        finally 
        { 
            _overlayService.SetClosable(true);
            IsLoggingIn = false; 
        }
    }
    
    //Commands
    [RelayCommand]
    private void CloseSelf() =>
        WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
    
    [RelayCommand]
    private async Task MicrosoftLogin() =>
        await ExecuteMicrosoftLogin();
    
    [RelayCommand]
    private void OfflineLogin(object parameter) =>
        ExecuteOffileLogin(parameter);
    
    [RelayCommand]
    private void AddNewAccount() =>
        IsAddAccPageOpen = true;

    [RelayCommand]
    private void SelectAccount(object parameter)
    {
        if (parameter is UserAccount account)
        {
            _identityStore.CurrentAccount = account;
        }
    }
    
    [RelayCommand]
    private void DeleteAccount(object parameter)
    {
        if (parameter is UserAccount account)
        {
            HandleDeleteAccount(account);
        }
    }

    [RelayCommand]
    private void RenameAccount(object parameter)
    {
        if (parameter is UserAccount account)
        {
            HandleRenameAccount(account);
        }
    }

}
