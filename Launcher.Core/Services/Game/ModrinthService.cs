using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Launcher.Core.Enums;
using Launcher.Core.Models;

namespace Launcher.Core.Services.Game
{
    public interface IModrinthService
    {
        Task InstallEssentialApisAsync(MinecraftInstance instance, string modsFolder, IProgress<LaunchState> progress = null);
        Task InstallPerformanceModsAsync(MinecraftInstance instance, string modsFolder, IProgress<LaunchState> progress = null);
    }

    public class ModrinthService : IModrinthService
    {
        private readonly HttpClient _httpClient;

        public ModrinthService()
        {
            _httpClient = new HttpClient();
            // ВАЖНО: Modrinth требует понятный User-Agent, иначе забанит
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MineLauncher/dev (vviktor2007@gmail.com)");
        }
        
        public async Task InstallEssentialApisAsync(MinecraftInstance instance, string modsFolder, IProgress<LaunchState> progress = null)
        {
            if (instance.IsolationType == IsolationType.Global) return;

            string slug = null;
            if (instance.LoaderType == GameLoaderType.Fabric) slug = "fabric-api";
            else if (instance.LoaderType == GameLoaderType.Quilt) slug = "qsl";

            if (slug == null) return;

            try
            {
                progress?.Report(new LaunchState { Progress = 5, StatusText = $"Downloading {slug}..." });
                var downloadUrl = await GetLatestModDownloadUrlAsync(slug, instance.GameVersion, instance.LoaderType.ToString().ToLower());
                if (downloadUrl != null)
                {
                    await DownloadModAsync(downloadUrl, modsFolder, $"{slug}-{instance.GameVersion}.jar");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Modrinth] Failed to install {slug}: {ex.Message}");
            }
        }
        
        public async Task InstallPerformanceModsAsync(MinecraftInstance instance, string modsFolder, IProgress<LaunchState> progress = null)
        {
            if (instance.IsolationType == IsolationType.Global) return;
            if (!instance.RequestPerformanceMods) return;

            string[] targets = { "sodium", "iris" };
            string loaderStr = instance.LoaderType.ToString().ToLower();

            progress?.Report(new LaunchState { Progress = 10, StatusText = "Downloading performance mods..." });

            foreach (var slug in targets)
            {
                progress?.Report(new LaunchState { Progress = 10, StatusText = $"Downloading {slug}..." });
                var downloadUrl = await GetLatestModDownloadUrlAsync(slug, instance.GameVersion, loaderStr);
                
                if (downloadUrl == null)
                    throw new InvalidOperationException($"Мод {slug} недоступен для Minecraft {instance.GameVersion} на лоадере {instance.LoaderType}.");

                await DownloadModAsync(downloadUrl, modsFolder, $"{slug}-{instance.GameVersion}.jar");
            }
        }

        // --- УНИВЕРСАЛЬНЫЕ МЕТОДЫ (ЗАГОТОВКА НА БУДУЩЕЕ) ---

        private async Task<string> GetLatestModDownloadUrlAsync(string slug, string gameVersion, string loader)
        {
            // Формируем запрос с фильтрацией по версии игры и лоадеру
            // Modrinth API ожидает массивы в формате JSON-строки, например: ["1.21.1"]
            string url = $"https://api.modrinth.com/v2/project/{slug}/version" +
                         $"?game_versions=[\"{gameVersion}\"]" +
                         $"&loaders=[\"{loader}\"]";

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(json);

            // API возвращает массив версий, отсортированных от новых к старым. Берем самую первую.
            var rootArray = document.RootElement;
            if (rootArray.GetArrayLength() == 0) return null; // Мод не найден под эти параметры

            var latestVersion = rootArray[0];
            var filesArray = latestVersion.GetProperty("files");
            
            // Возвращаем прямую ссылку на скачивание первого файла (обычно это primary .jar)
            return filesArray[0].GetProperty("url").GetString();
        }

        private async Task DownloadModAsync(string url, string folder, string fileName)
        {
            Directory.CreateDirectory(folder);
            var filePath = Path.Combine(folder, fileName);

            // Простая защита от повторного скачивания, если файл почему-то уже лежит
            if (File.Exists(filePath)) return;

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var fs = new FileStream(filePath, FileMode.Create);
            await response.Content.CopyToAsync(fs);
            Console.WriteLine($"[Modrinth] Downloaded {fileName} to {folder}");
        }
    }
}