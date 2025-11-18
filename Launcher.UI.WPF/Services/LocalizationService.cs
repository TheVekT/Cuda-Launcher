using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace Launcher.UI.WPF.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        public static LocalizationService Instance { get; } = new LocalizationService();
        
        private Dictionary<string, string> _translations = new Dictionary<string, string>();
        
        public LocalizationService()
        {
            LoadLanguage("en-US"); 
        }
        
        public string this[string key]
        {
            get
            {
                if (_translations.TryGetValue(key, out var value))
                {
                    return value;
                }
                return $"#{key}#";
            }
        }
        
        private class LanguageFile
        {
            public Dictionary<string, string> Meta { get; set; }
            public Dictionary<string, string> Translations { get; set; }
        }

        public void LoadLanguage(string langCode)
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Languages", $"{langCode}.json");

            if (File.Exists(filePath))
            {
                try 
                {
                    var json = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                    
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    
                    var langData = JsonSerializer.Deserialize<LanguageFile>(json, options);
                    
                    _translations = langData?.Translations ?? new Dictionary<string, string>();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading language: {ex.Message}");
                    _translations = new Dictionary<string, string>();
                }
            }
            else
            {
                _translations.Clear();
            }
    
            OnPropertyChanged("Item[]");
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}