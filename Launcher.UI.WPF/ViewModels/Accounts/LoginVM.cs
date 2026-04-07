using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Launcher.Core.Models;
using Launcher.Core.Services.Auth;
using Launcher.Core.Services.IO;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Services;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels.Accounts;
public partial class LoginVM: ObservableObject
{ 
    //Services
    private readonly IAuthService _authService;
    private readonly IAccountStorageService _accountStorage;
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
    //Events
    public event Action RequestClose;
        
    //Commands
    public ICommand MicrosoftLoginCommand { get; }
    public ICommand OfflineLoginCommand { get; }
    public ICommand SelectAccountCommand { get; }
    public ICommand CloseSelfCommand { get; }
    public ICommand AddNewAccountCommand { get; }
    public ICommand RenameAccountCommand { get; }
    public ICommand LogOutCommand { get; }
    



    public LoginVM(IAuthService authService, IAccountStorageService accountStorage,LoginStore loginStore)
    { 
        _authService = authService; 
        _accountStorage = accountStorage; 
        _loginStore = loginStore;
            
        AccountCount = _loginStore.Accounts.Count;
            
        MicrosoftLoginCommand = new RelayCommand(async (o) => await ExecuteMicrosoftLogin());
            
        OfflineLoginCommand = new RelayCommand(o => { ExecuteOffileLogin(o); });
            
        CloseSelfCommand = new RelayCommand(o => RequestClose?.Invoke());
            
        AddNewAccountCommand = new RelayCommand(o => IsAddAccPageOpen = true); 
            
        SelectAccountCommand = new RelayCommand(o => 
        {
            if (o is UserAccount account)
            {
                _loginStore.CurrentAccount = account;
            }
        });
            
        LogOutCommand = new RelayCommand(o => HandleLogout(o as UserAccount));
            
        RenameAccountCommand = new RelayCommand(o => HandleRenameAccount(o as UserAccount));
            
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
                
            RequestClose?.Invoke();
        }
    }


    private async Task ExecuteMicrosoftLogin()
    {
        if (IsLoggingIn) return;

        IsLoggingIn = true;
        try
        {
            var newAccount = await _authService.LoginWithMicrosoftAsync();
                
            _loginStore.RegisterLogin(newAccount);
            var title = LocalizationService.Instance["Success.LoginMicrosoftTitle"];
            var desc = LocalizationService.Instance["Success.LoginMicrosoftDesc"];
            NotificationService.Instance.ShowSuccess(title, desc);
            RequestClose?.Invoke();
        }
        catch (Exception ex) 
        { 
            var title = LocalizationService.Instance["Errors.LoginMicrosoftTitle"];
            var desc = LocalizationService.Instance["Errors.LoginMicrosoftDesc"];
            Console.WriteLine(ex.Message); 
        }
        finally 
        { 
            IsLoggingIn = false; 
        }
    }
    
}
