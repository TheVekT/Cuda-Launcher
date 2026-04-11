using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Messages;
using Launcher.Core.Models;
using Launcher.Core.Services.Auth;
using Launcher.Core.Services.IO;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels.Accounts;
public partial class LoginVM: ObservableObject
{ 
    //Services
    private readonly IAuthService _authService;
    private readonly IAccountStorageService _accountStorage;
    private readonly IOverlayService _overlayService;
    //Stores
    private readonly LoginStore _loginStore;
    //Attributes
    public LoginStore LoginStore => _loginStore;
    
    [ObservableProperty]
    private bool _isAddAccPageOpen;
    [ObservableProperty]
    private bool _isLoggingIn;
    [ObservableProperty]
    private int _accountCount = 0;

    public LoginVM(IAuthService authService, 
        IAccountStorageService accountStorage,
        IOverlayService overlayService,
        LoginStore loginStore)
    { 
        _authService = authService; 
        _accountStorage = accountStorage;
        _overlayService = overlayService;
        _loginStore = loginStore;
            
        AccountCount = _loginStore.Accounts.Count;
        
            
        _loginStore.Accounts.CollectionChanged += (s, e) => 
        {
            AccountCount = _loginStore.Accounts.Count;
        };
    }
        
    private void HandleRenameAccount(UserAccount account)
    {
        if (account.AccountTypeString == "Microsoft")
        {
            //Open URL https://www.minecraft.net/en-us/msaprofile/mygames/editprofile
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://www.minecraft.net/en-us/msaprofile/mygames/editprofile",
                UseShellExecute = true
            });
        }
        return;
    }
        
    private void HandleLogout(UserAccount account)
    {
        Console.WriteLine($"Logout {account.Username}");
        var isSelectedAccountToDelete = _loginStore.CurrentAccount == account;
        _loginStore.Accounts.Remove(account);
        _accountStorage.SaveAccounts(_loginStore.Accounts);
            
        if (isSelectedAccountToDelete && _loginStore.Accounts.Count > 0)
        {
            _loginStore.CurrentAccount = _loginStore.Accounts.FirstOrDefault();;
        }
        else if (_loginStore.Accounts.Count > 0)
        {
            _loginStore.CurrentAccount = _loginStore.CurrentAccount; 
        }
        else
        {
            IsAddAccPageOpen = true;
            _loginStore.CurrentAccount = null;
        }
    }
        

    private void ExecuteOffileLogin(object o)
    {
        if (o is string nickname && !string.IsNullOrWhiteSpace(nickname))
        {
            var account = _authService.LoginOffline(nickname);
                
            _loginStore.RegisterLogin(account); 
                
            WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
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
                
            _loginStore.RegisterLogin(newAccount);
            var title = LocalizationService.Instance["Success.LoginMicrosoftTitle"];
            var desc = LocalizationService.Instance["Success.LoginMicrosoftDesc"];
            NotificationService.Instance.ShowSuccess(title, desc);
            _overlayService.SetClosable(true);
            WeakReferenceMessenger.Default.Send(new CloseOverlayMessage());
            WeakReferenceMessenger.Default.Send(new MicrosoftLoggedMessage(newAccount));
        }
        catch (Exception ex) 
        { 
            var title = LocalizationService.Instance["Errors.LoginMicrosoftTitle"];
            var desc = LocalizationService.Instance["Errors.LoginMicrosoftDesc"];
            NotificationService.Instance.ShowError(title, desc);
            Console.WriteLine(ex.Message); 
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
            _loginStore.CurrentAccount = account;
        }
    }
    
    [RelayCommand]
    private void LogOut(object parameter)
    {
        if (parameter is UserAccount account)
        {
            HandleLogout(account);
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
