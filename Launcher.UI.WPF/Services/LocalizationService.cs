using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using CommunityToolkit.Mvvm.Messaging;
using Launcher.Core.Models;
using Launcher.Core.Services.System;
using Launcher.Core.Services.UI;
using Launcher.UI.WPF.Messages;
using Launcher.UI.WPF.Models;

namespace Launcher.UI.WPF.Services;

public class LocalizationService : ILocalizationService, INotifyPropertyChanged
{
    public static LocalizationService Instance { get; internal set; }

    private Dictionary<string, string> _translations = new Dictionary<string, string>();
    
    private readonly string _languagesRoot;
    
    private readonly ILauncherPathsService _pathsService;

    public LocalizationService Current => this;

    public LocalizationService(ILauncherPathsService pathsService)
    {
        _pathsService = pathsService;
        
        _languagesRoot = Path.Combine(_pathsService.AssetsDirectory, "Languages");

        if (!Directory.Exists(_languagesRoot))
        {
            Directory.CreateDirectory(_languagesRoot);
        }

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

    public async Task ImportLocalization(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            var destPath = Path.Combine(_languagesRoot, fileName);

            File.Copy(filePath, destPath, overwrite: true);
            WeakReferenceMessenger.Default.Send(new LanguageImportedMessage());
        }
        catch
        {
            // Игнорируем любые ошибки при копировании файла
        }
    }

    public List<LanguageModel> GetAvailableLanguages()
    {
        var languages = new List<LanguageModel>();

        if (!Directory.Exists(_languagesRoot))
        {
            // Базовый фоллбэк, если папка не найдена
            languages.Add(new LanguageModel { Name = "English", Code = "en-US" });
            return languages;
        }

        var files = Directory.GetFiles(_languagesRoot, "*.json");

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file, Encoding.UTF8);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var langData = JsonSerializer.Deserialize<LanguageFile>(json, options);

                if (langData?.Meta != null)
                {
                    // Достаем имя и код из метадата
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
        
        return languages.OrderBy(l => l.Name).ToList();
    }

    public string GetCodeByName(string name)
    {
        if (!Directory.Exists(_languagesRoot)) return "en-US";

        var files = Directory.GetFiles(_languagesRoot, "*.json");

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
        string foundPath = Path.Combine(_languagesRoot, $"{langCode}.json");

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
