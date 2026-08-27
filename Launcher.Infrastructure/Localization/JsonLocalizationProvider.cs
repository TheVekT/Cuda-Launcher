using System.Text;
using System.Text.Json;
using Launcher.Core.Config.Abstractions;
using Launcher.Infrastructure.Localization.Abstractions;
using Launcher.Infrastructure.Localization.Models;

namespace Launcher.Infrastructure.Localization;

public class JsonLocalizationProvider : ILocalizationProvider
{
    private readonly ILauncherPathsService _pathsService;
    private readonly string _languagesRoot;
    private readonly string[] _builtInLanguageCodes = { "en-US" };

    public JsonLocalizationProvider(ILauncherPathsService pathsService)
    {
        _pathsService = pathsService;
        _languagesRoot = Path.Combine(_pathsService.AssetsDirectory, "Languages");

        if (!Directory.Exists(_languagesRoot))
        {
            Directory.CreateDirectory(_languagesRoot);
        }
    }

    public List<LanguageModel> GetAvailableLanguages()
    {
        var languages = new List<LanguageModel>();

        if (!Directory.Exists(_languagesRoot))
        {
            languages.Add(new LanguageModel { Name = "English (US)", Code = "en-US", Author = "TheVekT", Version = "1.0.0" });
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
                    langData.Meta.TryGetValue("Name", out var name);
                    langData.Meta.TryGetValue("Code", out var code);
                    langData.Meta.TryGetValue("Author", out var author);
                    langData.Meta.TryGetValue("Version", out var version);

                    if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(code))
                    {
                        languages.Add(new LanguageModel 
                        { 
                            Name = name, 
                            Code = code,
                            Author = author ?? string.Empty,
                            Version = version ?? "1.0.0"
                        });
                    }
                }
            }
            catch
            {
                // Если файл поврежден, пропускаем его
                continue;
            }
        }

        return languages
            .OrderByDescending(l => _builtInLanguageCodes.Contains(l.Code))
            .ThenBy(l => l.Name)
            .ToList();
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
                            if (langData.Meta.TryGetValue("Code", out var code) && !string.IsNullOrEmpty(code))
                            {
                                return code;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Если файл поврежден или занят другим процессом — пропускаем его
                continue;
            }
        }

        return "en-US";
    }

    public Dictionary<string, string> LoadTranslations(string langCode)
    {
        string foundPath = Path.Combine(_languagesRoot, $"{langCode}.json");

        if (File.Exists(foundPath))
        {
            try
            {
                var json = File.ReadAllText(foundPath, Encoding.UTF8);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var langData = JsonSerializer.Deserialize<LanguageFile>(json, options);
                return langData?.Translations ?? new Dictionary<string, string>();
            }
            catch
            {
                return new Dictionary<string, string>();
            }
        }

        return new Dictionary<string, string>();
    }

    public Task ImportLanguageAsync(string filePath)
    {
        try
        {
            var fileName = Path.GetFileName(filePath);
            var destPath = Path.Combine(_languagesRoot, fileName);

            File.Copy(filePath, destPath, overwrite: true);
        }
        catch
        {
            // Игнорируем ошибки при копировании файла
        }

        return Task.CompletedTask;
    }

    private class LanguageFile
    {
        public Dictionary<string, string>? Meta { get; set; }
        public Dictionary<string, string>? Translations { get; set; }
    }
}
