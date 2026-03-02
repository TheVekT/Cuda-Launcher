using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Launcher.Core.Models;
using Launcher.Core.Services.Auth;
using Launcher.Core.Services.IO;

namespace Launcher.UI.WPF.Stores
{
    public class LoginStore : INotifyPropertyChanged
    {
        private readonly IAccountStorageService _accountStorage;
        private readonly ISettingsService _settingsService;
        private readonly IAuthService _authService;
        
        private bool _isLoggingIn;
        private UserAccount _currentAccount;
        private string _userName = "Guest";
        private string _lastSelectedAccountUUID; // ID для сохранения в settings.json
        
        public bool IsLoggedIn => CurrentAccount != null;
        public ObservableCollection<UserAccount> Accounts { get; set; } = new();

        // Добавили ISettingsService в конструктор
        public LoginStore(IAccountStorageService accountStorage, ISettingsService settingsService, IAuthService authService)
        {
            _accountStorage = accountStorage;
            _settingsService = settingsService;
            _authService = authService;
            
            // Загружаем настройки (это восстановит LastSelectedAccountUUID, если он есть)
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
            
            // Сохраняем файл с токенами только при регистрации/обновлении
            _accountStorage.SaveAccounts(Accounts);
        }
        
        private void LoadSavedAccounts()
        {
            var savedAccounts = _accountStorage.LoadAccounts();
            Accounts.Clear();
            foreach (var acc in savedAccounts) Accounts.Add(acc);
        
            // Ищем по UUID, который загрузился из settings.json
            if (!string.IsNullOrEmpty(LastSelectedAccountUUID))
            {
                var lastUsedAccount = Accounts.FirstOrDefault(x => x.UUID == LastSelectedAccountUUID);
                if (lastUsedAccount != null)
                {
                    CurrentAccount = lastUsedAccount;
                    return;
                }
            }
            
            if (Accounts.Count > 0) CurrentAccount = Accounts.First();
        }
        
        //Getters and Setters
        
        [SettingProperty]
        public string LastSelectedAccountUUID
        {
            get => _lastSelectedAccountUUID;
            set
            {
                if (_lastSelectedAccountUUID != value)
                {
                    _lastSelectedAccountUUID = value;
                    OnPropertyChanged(nameof(LastSelectedAccountUUID));
                }
            }
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
                        
                        // Сохраняем выбранный UUID. Умный сервис сам запишет это в settings.json!
                        LastSelectedAccountUUID = _currentAccount.UUID;
                    }
                }
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}