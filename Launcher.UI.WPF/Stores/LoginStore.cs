using System.Collections.ObjectModel;
using System.ComponentModel;
using Launcher.Core.Models;
using Launcher.Core.Services.IO;


namespace Launcher.UI.WPF.Stores
{
    public class LoginStore : INotifyPropertyChanged
    {
        private readonly IAccountStorageService _accountStorage;
        
        private bool _isLoggingIn;
        private UserAccount _currentAccount;
        private string _userName = "Guest";
        
        public bool IsLoggedIn => CurrentAccount != null;
        public ObservableCollection<UserAccount> Accounts { get; set; } = new();

        public LoginStore(IAccountStorageService accountStorage)
        {
            _accountStorage = accountStorage;
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
            Accounts.Clear();
            foreach (var acc in savedAccounts) Accounts.Add(acc);
        
            var lastUsedAccount = Accounts.FirstOrDefault(x => x.IsSelected);
            if (lastUsedAccount != null) CurrentAccount = lastUsedAccount;
            else if (Accounts.Count > 0) CurrentAccount = Accounts.First();
        }
        
        //Getters and Setters
        public string UserName
        {
            get => _userName;
            set { _userName = value; OnPropertyChanged(nameof(UserName)); }
        }

        public bool IsLoggingIn
        {
            get => _isLoggingIn;
            set { _isLoggingIn = value; OnPropertyChanged(nameof(IsLoggingIn)); }
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
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}