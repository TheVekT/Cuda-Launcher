using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Launcher.Core.Models;

namespace Launcher.Core.Services.IO
{
    public interface IAccountStorageService
    {
        void SaveAccounts(IEnumerable<UserAccount> accounts);
        List<UserAccount> LoadAccounts();
    }

    public class AccountStorageService : IAccountStorageService
    {
        private readonly string _userDataPath; // Путь к папке Data/UserData
        private readonly string _filePath;

        public AccountStorageService()
        {
            // Формируем путь: .../Data/UserData
            _userDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "UserData");
            _filePath = Path.Combine(_userDataPath, "accounts.json");
        }

        public void SaveAccounts(IEnumerable<UserAccount> accounts)
        {
            // Создаем папку Data/UserData, если нет
            if (!Directory.Exists(_userDataPath))
            {
                Directory.CreateDirectory(_userDataPath);
            }

            try 
            {
                var json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving accounts: {ex.Message}");
            }
        }

        public List<UserAccount> LoadAccounts()
        {
            if (!File.Exists(_filePath))
            {
                return new List<UserAccount>();
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var accounts = JsonSerializer.Deserialize<List<UserAccount>>(json);
                return accounts ?? new List<UserAccount>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading accounts: {ex.Message}");
                return new List<UserAccount>();
            }
        }
    }
}