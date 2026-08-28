using System.Diagnostics;
using System.Text.Json;
using Launcher.Core.Common.Enums;
using Launcher.Core.Common.Models;
using Launcher.Core.Instances.Models;
using Launcher.Core.Mods.Abstractions;

namespace Launcher.Core.Mods;

public class ModrinthService : IModrinthService
{
    private readonly HttpClient _httpClient;

    public ModrinthService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CudaLauncher/dev (vviktor2007@gmail.com)");
    }
    
    public async Task<bool> InstallEssentialApisAsync(MinecraftInstance instance, string modsFolder, IProgress<LaunchState> progress)
    {
        if (instance.IsolationType == IsolationType.Global) return false;
        
        string? slug = null;
        if (instance.LoaderType == GameLoaderType.Fabric) slug = "fabric-api";
        else if (instance.LoaderType == GameLoaderType.Quilt) slug = "qsl";
        if (slug == null) return false;

        try
        {
            progress.Report(new LaunchState { Progress = 5, StatusText = $"Downloading {slug}..." });
            var downloadUrl = await GetLatestModDownloadUrlAsync(slug, instance.GameVersion, instance.LoaderType.ToString().ToLower());
            if (downloadUrl != null)
                await DownloadModAsync(downloadUrl, modsFolder, $"{slug}-{instance.GameVersion}.jar");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Modrinth] Failed to install {slug}: {ex.Message}");
            return false;
        }
        return true;
    }
    
    public async Task<bool> InstallPerformanceModsAsync(MinecraftInstance instance, string modsFolder, IProgress<LaunchState> progress)
    {
        if (instance.IsolationType == IsolationType.Global) return false;
        if (!instance.RequestPerformanceMods) return false;

        string[] targets = ["sodium", "iris"];
        var loaderStr = instance.LoaderType.ToString().ToLower();

        progress.Report(new LaunchState { Progress = 10, StatusText = "Downloading performance mods..." });
        
        try
        {
            foreach (var slug in targets)
            {
                progress?.Report(new LaunchState { Progress = 10, StatusText = $"Downloading {slug}..." });
                var downloadUrl = await GetLatestModDownloadUrlAsync(slug, instance.GameVersion, loaderStr);
            
                if (downloadUrl == null)
                    throw new InvalidOperationException($"Мод {slug} недоступен для Minecraft {instance.GameVersion} на лоадере {instance.LoaderType}.");

                await DownloadModAsync(downloadUrl, modsFolder, $"{slug}-{instance.GameVersion}.jar");
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return false;
        }
        
        return true;
    }

    private async Task<string?> GetLatestModDownloadUrlAsync(string slug, string gameVersion, string loader)
    {
        // Form the Modrinth API URL to fetch the latest version of the mod for the specified game version and loader.
        // Modrinth API expects arrays in the format of a JSON string, e.g., ["1.21.1"]
        string url = $"https://api.modrinth.com/v2/project/{slug}/version" +
                     $"?game_versions=[\"{gameVersion}\"]" +
                     $"&loaders=[\"{loader}\"]";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        
        var rootArray = document.RootElement;
        
        if (rootArray.GetArrayLength() == 0) return null; // No versions found for the specified criteria

        var latestVersion = rootArray[0];
        var filesArray = latestVersion.GetProperty("files");
        
        // Assuming the first file is the main mod file. You might want to add additional checks here.
        return filesArray[0].GetProperty("url").GetString();
    }

    private async Task DownloadModAsync(string url, string folder, string fileName)
    {
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, fileName);
        
        if (File.Exists(filePath)) return;

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        await using var fs = new FileStream(filePath, FileMode.Create);
        await response.Content.CopyToAsync(fs);
        Console.WriteLine($"[Modrinth] Downloaded {fileName} to {folder}");
    }
}
