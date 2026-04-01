using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; 
using System.Text.Json;
using Launcher.Core.Helpers; 
using Launcher.Core.Models;
using Launcher.Core.Services.System;

namespace Launcher.Core.Services.IO;

public interface IAccountStorageService
{
    void SaveAccounts(IEnumerable<UserAccount> accounts);
    List<UserAccount> LoadAccounts();
}

public class AccountStorageService : IAccountStorageService
{
    private readonly string _userDataPath;
    private readonly string _filePath;
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
