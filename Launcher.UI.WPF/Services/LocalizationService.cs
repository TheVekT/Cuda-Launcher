using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.ComponentModel;

namespace Launcher.UI.WPF.Services
{
    public class LocalizationService : INotifyPropertyChanged
    {
        public static LocalizationService Instance { get; } = new LocalizationService();

        private Dictionary<string, string> _translations = new Dictionary<string, string>();

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

                if (string.IsNullOrEmpty(key)) return "";
                try
                {
                    var parts = key.Split('.');
                    return parts.Length > 0 ? parts.Last() : key;
                }
                catch
                {
                    return key;
                }
            }
        }

        public void LoadLanguage(string langCode)
        {
            string assetsRelativePath = Path.Combine("Assets", "Languages", $"{langCode}.json");
            string runtimePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, assetsRelativePath);

            string foundPath = null;

            if (File.Exists(runtimePath))
            {
                foundPath = runtimePath;
            }

            if (foundPath == null)
            {
                foundPath = FindFileViaSourceAnchor(assetsRelativePath);
            }

            if (foundPath != null)
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
                    try
                    {
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        var json = File.ReadAllText(foundPath, Encoding.GetEncoding(1251));
                        var langData = JsonSerializer.Deserialize<LanguageFile>(json);
                        _translations = langData?.Translations ?? new Dictionary<string, string>();
                    }
                    catch { }
                }
            }
            else
            {
                _translations.Clear();
            }

            OnPropertyChanged("Item[]");
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