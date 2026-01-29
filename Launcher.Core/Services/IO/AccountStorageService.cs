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
        private readonly string _storagePath;
        private readonly string _filePath;

        public AccountStorageService()
        {
            // Portable путь: папка с .exe + папка Data
            _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _filePath = Path.Combine(_storagePath, "accounts.json");
        }

        public void SaveAccounts(IEnumerable<UserAccount> accounts)
        {
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }

            var json = JsonSerializer.Serialize(accounts, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            File.WriteAllText(_filePath, json);
        }

        public List<UserAccount> LoadAccounts()
        {
            if (!File.Exists(_filePath))
            {
                Console.WriteLine("File not found: " + _filePath);
                return new List<UserAccount>();
            }

            try
            {
                Console.WriteLine("Loading accounts from file: " + _filePath);
                var json = File.ReadAllText(_filePath);
                var accounts = JsonSerializer.Deserialize<List<UserAccount>>(json);
                return accounts ?? new List<UserAccount>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error loading accounts from file: " + _filePath);
                Console.WriteLine("Exception: " + ex.Message);
                // Если файл поврежден, возвращаем пустой список
                return new List<UserAccount>();
            }
        }
    }
}