using System.Text.Json;
using Launcher.Core.Common.Helpers;
using Launcher.Core.Config.Abstractions;
using Launcher.Core.Identity.Abstractions;
using Launcher.Core.Identity.Models;

namespace Launcher.Core.Identity;

public class AccountStorageService : IAccountStorageService
{
    private readonly string _userDataPath;
    private readonly string _filePath;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly ILauncherPathsService _pathsService;

    public AccountStorageService(ILauncherPathsService pathsService)
    {
        _pathsService = pathsService;
        
        _userDataPath = _pathsService.UserDataDirectory;
        _filePath = Path.Combine(_userDataPath, "accounts.json");
    }

    public void SaveAccounts(IEnumerable<UserAccount> accounts)
    {
        if (!Directory.Exists(_userDataPath)) Directory.CreateDirectory(_userDataPath);

        try 
        {
            var accountsToSave = accounts.Select(acc => new UserAccount
            {
                Username = acc.Username,
                UUID = acc.UUID,
                IsOffline = acc.IsOffline,
                
                AccessToken = !acc.IsOffline 
                    ? SecurityHelper.Protect(acc.AccessToken) 
                    : acc.AccessToken
            }).ToList();

            var json = JsonSerializer.Serialize(accountsToSave, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving accounts: {ex.Message}");
        }
    }

    public List<UserAccount> LoadAccounts()
    {
        if (!File.Exists(_filePath)) return new List<UserAccount>();

        try
        {
            var json = File.ReadAllText(_filePath);
            var loadedAccounts = JsonSerializer.Deserialize<List<UserAccount>>(json);

            if (loadedAccounts == null) return new List<UserAccount>();

            foreach (var acc in loadedAccounts)
            {
                if (!acc.IsOffline)
                {
                    var decryptedToken = SecurityHelper.Unprotect(acc.AccessToken);
                    acc.AccessToken = decryptedToken;
                }
                else if (string.IsNullOrEmpty(acc.AccessToken))
                {
                    acc.AccessToken = "access_token";
                }
            }
            return loadedAccounts;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading accounts: {ex.Message}");
            return new List<UserAccount>();
        }
    }
}
