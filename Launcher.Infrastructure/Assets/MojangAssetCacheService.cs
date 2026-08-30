using System.Diagnostics;
using Launcher.Core.Config.Abstractions;
using Launcher.Infrastructure.Assets.Abstractions;

namespace Launcher.Infrastructure.Assets;

public class MojangAssetCacheService : IMojangAssetCacheService
{
    private readonly HttpClient _httpClient;
    private readonly string _cacheDirectory;

    public MojangAssetCacheService(HttpClient httpClient, ILauncherPathsService pathsService)
    {
        _httpClient = httpClient;
        
        _cacheDirectory = Path.Combine(pathsService.CacheDirectory, "MojangAssets");
        if (!Directory.Exists(_cacheDirectory)) Directory.CreateDirectory(_cacheDirectory);
    }
    
    public async Task<string?> GetOrDownloadAssetAsync(string url, string assetId)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(assetId)) return null;
        
        var localPath = Path.Combine(_cacheDirectory, $"{assetId}.png");
        
        if (File.Exists(localPath))
        {
            return localPath;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(localPath, imageBytes);
            return localPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Cache] Failed to download asset: {ex.Message}");
            return null;
        }
    }
}