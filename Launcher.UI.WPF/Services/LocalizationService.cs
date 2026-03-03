
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Launcher.Core.Models;
using Launcher.Core.Services.UI;
using Launcher.UI.WPF.Models;

namespace Launcher.UI.WPF.Services
{
    public class LocalizationService : ILocalizationService
    {
        public static LocalizationService Instance { get; } = new LocalizationService();

        private Dictionary<string, string> _translations = new Dictionary<string, string>();
        private string _languagesDir;

        public LocalizationService Current => this;

        public LocalizationService()
        {
            LoadLanguage("en-US");
        }

        public string this[string key]
        {
            get
            {
                if (_translations.TryGetValue(key, out var value))
                    return value;
                return key;
            }
        }
        
        public List<LanguageModel> GetAvailableLanguages()
        {
            var languages = new List<LanguageModel>();
            var dir = GetLanguagesDirectory();

            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                // Базовый фоллбэк, если папка не найдена
                languages.Add(new LanguageModel { Name = "English", Code = "en-US" });
                return languages;
            }

            var files = Directory.GetFiles(dir, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file, Encoding.UTF8);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var langData = JsonSerializer.Deserialize<LanguageFile>(json, options);

                    if (langData?.Meta != null)
                    {
                        // Достаем имя и код из метадаты
                        langData.Meta.TryGetValue("Name", out var name);
                        langData.Meta.TryGetValue("LanguageCode", out var code);

                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(code))
                        {
                            languages.Add(new LanguageModel { Name = name, Code = code });
                        }
                    }
                }
                catch
                {
                    // Если файл битый, просто идем дальше
                    continue;
                }
            }

            // Возвращаем список, отсортированный по алфавиту для красоты
            return languages.OrderBy(l => l.Name).ToList();
        }

        public string GetCodeByName(string name)
        {
            var dir = GetLanguagesDirectory();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return "en-US";
            }

            var files = Directory.GetFiles(dir, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file, Encoding.UTF8);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var langData = JsonSerializer.Deserialize<LanguageFile>(json, options);

                    if (langData?.Meta != null)
                    {
                        if (langData.Meta.TryGetValue("Name", out var metaName) && 
                            !string.IsNullOrEmpty(metaName))
                        {
                            if (string.Equals(metaName, name, StringComparison.OrdinalIgnoreCase))
                            {
                                if (langData.Meta.TryGetValue("LanguageCode", out var code))
                                {
                                    return code;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Если файл битый или занят другим процессом — просто пропускаем его
                    continue;
                }
            }
            return "en-US";
        }

        public void LoadLanguage(string langCode)
        {
            var dir = GetLanguagesDirectory();
            string foundPath = null;

            if (!string.IsNullOrEmpty(dir))
            {
                foundPath = Path.Combine(dir, $"{langCode}.json");
            }

            if (File.Exists(foundPath))
            {
                try
                {
                    var json = File.ReadAllText(foundPath, Encoding.UTF8);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var langData = JsonSerializer.Deserialize<LanguageFile>(json, options);
                    _translations = langData?.Translations ?? new Dictionary<string, string>();
                }
                catch
                {
                     // Fallback logic for encoding if needed...
                    _translations = new Dictionary<string, string>();
                }
            }
            else
            {
                _translations.Clear();
            }

            OnPropertyChanged("Item[]");
        }
        
        private string GetLanguagesDirectory()
        {
            if (!string.IsNullOrEmpty(_languagesDir)) return _languagesDir;

            string assetsRelativePath = Path.Combine("Assets", "Languages");
            string runtimePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assetsRelativePath);

            if (Directory.Exists(runtimePath))
            {
                _languagesDir = runtimePath;
                return runtimePath;
            }
            
            string anchorFile = Path.Combine(assetsRelativePath, "en-US.json");
            string foundFile = FindFileViaSourceAnchor(anchorFile);
            
            if (!string.IsNullOrEmpty(foundFile))
            {
                _languagesDir = Path.GetDirectoryName(foundFile);
                return _languagesDir;
            }

            return null;
        }

        private string FindFileViaSourceAnchor(string relativePath)
        {
            try
            {
                string currentSourcePath = GetSourceFilePath();
                string currentDir = Path.GetDirectoryName(currentSourcePath);

                for (int i = 0; i < 6; i++)
                {
                    if (string.IsNullOrEmpty(currentDir)) break;

                    string tryPath = Path.Combine(currentDir, relativePath);

                    if (File.Exists(tryPath))
                    {
                        return tryPath;
                    }

                    var parent = Directory.GetParent(currentDir);
                    if (parent == null) break;
                    currentDir = parent.FullName;
                }
            }
            catch { }
            return null;
        }

        private static string GetSourceFilePath([CallerFilePath] string sourceFilePath = "")
        {
            return sourceFilePath;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class LanguageFile
        {
            public Dictionary<string, string> Meta { get; set; }
            public Dictionary<string, string> Translations { get; set; }
        }
    }
}