using System.ComponentModel;
using System.Windows.Input;
using Launcher.Core.Models;
using Launcher.Core.Services.Auth;
using Launcher.Core.Services.IO;
using Launcher.UI.WPF.Helpers;
using Launcher.UI.WPF.Stores;

namespace Launcher.UI.WPF.ViewModels{
    public class LoginVM: INotifyPropertyChanged
    {
        //Services
        private readonly IAuthService _authService;
        private readonly IAccountStorageService _accountStorage;
        //Stores
        private readonly LoginStore _loginStore;
        //Attributes
        public LoginStore LoginStore => _loginStore;
        private bool _isAddAccPageOpen;
        private bool _isLoggingIn;
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

        public int _accountCount = 0;


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
                    _accountStorage.SaveAccounts(_loginStore.Accounts); 
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
                
                RequestClose?.Invoke();
            }
            catch (Exception ex) 
            { 
                Console.WriteLine(ex.Message); 
            }
            finally 
            { 
                IsLoggingIn = false; 
            }
        }

        //Getters & Setters
        
        public bool IsLoggingIn 
        {
            get => _isLoggingIn;
            set { _isLoggingIn = value; OnPropertyChanged(nameof(IsLoggingIn)); }
        }
        public int AccountCount
        {
            get => _accountCount;
            set {
                if (_accountCount != value)
                {
                    _accountCount = value;
                    OnPropertyChanged(nameof(AccountCount));
                }
            }
        }
        
        public bool IsAddAccPageOpen
        {
            get => _isAddAccPageOpen;
            set { _isAddAccPageOpen = value; OnPropertyChanged(nameof(IsAddAccPageOpen)); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}