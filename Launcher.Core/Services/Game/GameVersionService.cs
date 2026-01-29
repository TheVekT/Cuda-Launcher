using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.VersionMetadata; 
using CmlLib.Core.Installer.Forge; 
using CmlLib.Core.Installer.NeoForge; 
using Launcher.Core.Models;

namespace Launcher.Core.Services.Game
{
    public class GameVersionService : IGameVersionService
    {
        private readonly MinecraftLauncher _launcher;
        private readonly HttpClient _httpClient;

        // --- КЕШ (ОПЕРАТИВНАЯ ПАМЯТЬ) ---
        private IEnumerable<string> _cachedVanillaVersions;
        private IEnumerable<string> _cachedForgeSupportedVersions;
        private IEnumerable<string> _cachedNeoForgeSupportedVersions;
        private IEnumerable<string> _cachedFabricSupportedVersions;
        private IEnumerable<string> _cachedQuiltSupportedVersions;

        public GameVersionService()
        {
            var path = new MinecraftPath(); 
            _launcher = new MinecraftLauncher(path);
            _httpClient = new HttpClient();
        }

        public async Task<IEnumerable<string>> GetVanillaVersionsAsync()
        {
            if (_cachedVanillaVersions != null) return _cachedVanillaVersions;

            try
            {
                var versions = await _launcher.GetAllVersionsAsync();

                _cachedVanillaVersions = versions
                    .Where(v => v.GetVersionType() == MVersionType.Release)
                    .Select(v => v.Name)
                    .ToList();

                return _cachedVanillaVersions;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching vanilla versions: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<IEnumerable<string>> GetLoaderVersionsAsync(GameLoaderType type, string gameVersion)
        {
            if (string.IsNullOrWhiteSpace(gameVersion)) return new List<string>();

            try
            {
                switch (type)
                {
                    case GameLoaderType.Vanilla:
                        return new List<string> { gameVersion };

                    case GameLoaderType.Forge:
                        return await GetForgeVersions(gameVersion);

                    case GameLoaderType.NeoForge:
                        return await GetNeoForgeVersions(gameVersion);

                    case GameLoaderType.Fabric:
                        return await GetFabricVersions(gameVersion);

                    case GameLoaderType.Quilt:
                        return await GetQuiltVersions(gameVersion);

                    default:
                        return new List<string>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching loader versions for {type}: {ex.Message}");
                return new List<string>();
            }
        }

        // --- ПОЛУЧЕНИЕ КОНКРЕТНЫХ ВЕРСИЙ ЗАГРУЗЧИКОВ ---

        private async Task<IEnumerable<string>> GetForgeVersions(string gameVersion)
        {
            try
            {
                var forgeInstaller = new ForgeInstaller(_launcher);
                var versions = await forgeInstaller.GetForgeVersions(gameVersion);
                return versions.Select(v => v.ForgeVersionName);
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task<IEnumerable<string>> GetNeoForgeVersions(string gameVersion)
        {
            try 
            {
                var neoInstaller = new NeoForgeInstaller(_launcher);
                // Исправлено: GetVersions вместо GetForgeVersions для NeoForge
                var versions = await neoInstaller.GetForgeVersions(gameVersion);
                // Обычно Name содержит полную версию
                return versions.Select(v => v.VersionName); 
            }
            catch
            {
                return new List<string>();
            }
        }

        private async Task<IEnumerable<string>> GetFabricVersions(string gameVersion)
        {
            var url = $"https://meta.fabricmc.net/v2/versions/loader/{gameVersion}";
            // Можно добавить простой try-catch или кеширование словарем, но пока оставим прямым запросом
            // так как это зависит от gameVersion, кешировать сложнее (нужен Dictionary)
            try 
            {
                var json = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var list = new List<string>();

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var loaderObj = element.GetProperty("loader");
                    var version = loaderObj.GetProperty("version").GetString();
                    list.Add(version);
                }
                return list;
            }
            catch { return new List<string>(); }
        }

        private async Task<IEnumerable<string>> GetQuiltVersions(string gameVersion)
        {
            var url = $"https://meta.quiltmc.org/v3/versions/loader/{gameVersion}";
            try
            {
                var json = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(json);
                var list = new List<string>();

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    var loaderObj = element.GetProperty("loader");
                    var version = loaderObj.GetProperty("version").GetString();
                    list.Add(version);
                }
                return list;
            }
            catch { return new List<string>(); }
        }

        // --- ПОЛУЧЕНИЕ СПИСКА ВЕРСИЙ ИГРЫ ПО ТИПУ ЗАГРУЗЧИКА ---

        public async Task<IEnumerable<string>> GetGameVersionsByTypeAsync(GameLoaderType type)
        {
            switch (type)
            {
                case GameLoaderType.Vanilla:
                    return await GetVanillaVersionsAsync(); // Уже с кешем

                case GameLoaderType.Forge:
                    return await GetForgeSupportedMcVersions(); // Добавлен кеш

                case GameLoaderType.NeoForge:
                    return await GetNeoForgeSupportedMcVersions(); // Добавлен кеш

                case GameLoaderType.Fabric:
                    return await GetFabricSupportedMcVersions(); // Добавлен кеш

                case GameLoaderType.Quilt:
                    return await GetQuiltSupportedMcVersions(); // Добавлен кеш

                default:
                    return await GetVanillaVersionsAsync();
            }
        }

        // --- ПРИВАТНЫЕ МЕТОДЫ ПОИСКА ВЕРСИЙ ИГРЫ (С КЕШИРОВАНИЕМ) ---

        private async Task<IEnumerable<string>> GetForgeSupportedMcVersions()
        {
            // 1. Проверка кеша
            if (_cachedForgeSupportedVersions != null) return _cachedForgeSupportedVersions;

            try
            {
                var url = "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json";
                var json = await _httpClient.GetStringAsync(url);
                
                using var doc = JsonDocument.Parse(json);
                var promos = doc.RootElement.GetProperty("promos");
                
                var versions = new HashSet<string>();

                foreach (var property in promos.EnumerateObject())
                {
                    var key = property.Name;
                    var dashIndex = key.IndexOf('-');
                    if (dashIndex > 0)
                    {
                        var mcVersion = key.Substring(0, dashIndex);
                        versions.Add(mcVersion);
                    }
                }

                var vanilla = await GetVanillaVersionsAsync();
                
                // 2. Запись в кеш
                _cachedForgeSupportedVersions = vanilla.Where(v => versions.Contains(v)).ToList();
                return _cachedForgeSupportedVersions;
            }
            catch
            {
                return await GetVanillaVersionsAsync();
            }
        }

        private async Task<IEnumerable<string>> GetNeoForgeSupportedMcVersions()
        {
            // 1. Проверка кеша
            if (_cachedNeoForgeSupportedVersions != null) return _cachedNeoForgeSupportedVersions;

            try
            {
                var url = "https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge";
                var json = await _httpClient.GetStringAsync(url);
                
                using var doc = JsonDocument.Parse(json);
                var versionsArray = doc.RootElement.GetProperty("versions");
                
                var supportedMcVersions = new HashSet<string>();

                foreach (var v in versionsArray.EnumerateArray())
                {
                    var verStr = v.GetString();
                    if (verStr.StartsWith("1.")) 
                    {
                        var dashIndex = verStr.IndexOf('-');
                        if (dashIndex > 0)
                            supportedMcVersions.Add(verStr.Substring(0, dashIndex));
                    }
                    else 
                    {
                        var parts = verStr.Split('.');
                        if (parts.Length >= 2 && int.TryParse(parts[0], out int major) && int.TryParse(parts[1], out int minor))
                        {
                            if (minor == 0) supportedMcVersions.Add($"1.{major}");
                            else supportedMcVersions.Add($"1.{major}.{minor}");
                        }
                    }
                }

                var vanilla = await GetVanillaVersionsAsync();
                
                // 2. Запись в кеш
                _cachedNeoForgeSupportedVersions = vanilla.Where(v => supportedMcVersions.Contains(v)).ToList();
                return _cachedNeoForgeSupportedVersions;
            }
            catch
            {
                // Фолбэк для NeoForge (1.20.1+)
                var vanilla = await GetVanillaVersionsAsync();
                return vanilla.Where(v => string.Compare(v, "1.20.1", StringComparison.Ordinal) >= 0 || v.StartsWith("1.2"));
            }
        }

        private async Task<IEnumerable<string>> GetFabricSupportedMcVersions()
        {
            // 1. Проверка кеша
            if (_cachedFabricSupportedVersions != null) return _cachedFabricSupportedVersions;

            try 
            {
                var url = "https://meta.fabricmc.net/v2/versions/game";
                var json = await _httpClient.GetStringAsync(url);
                
                using var doc = JsonDocument.Parse(json);
                var list = new List<string>();

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("stable", out var stable) && stable.GetBoolean())
                    {
                        list.Add(element.GetProperty("version").GetString());
                    }
                }
                
                // 2. Запись в кеш
                _cachedFabricSupportedVersions = list;
                return _cachedFabricSupportedVersions;
            }
            catch 
            {
                return await GetVanillaVersionsAsync();
            }
        }

        private async Task<IEnumerable<string>> GetQuiltSupportedMcVersions()
        {
            // 1. Проверка кеша
            if (_cachedQuiltSupportedVersions != null) return _cachedQuiltSupportedVersions;

            try 
            {
                var url = "https://meta.quiltmc.org/v3/versions/game";
                var json = await _httpClient.GetStringAsync(url);
                
                using var doc = JsonDocument.Parse(json);
                var list = new List<string>();

                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("stable", out var stable) && stable.GetBoolean())
                    {
                        list.Add(element.GetProperty("version").GetString());
                    }
                }

                // 2. Запись в кеш
                _cachedQuiltSupportedVersions = list;
                return _cachedQuiltSupportedVersions;
            }
            catch 
            {
                return await GetVanillaVersionsAsync();
            }
        }
    }
}