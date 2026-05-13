using System.Diagnostics;
using Launcher.Core.Assets.Abstractions;
using Launcher.Core.Config.Abstractions;

namespace Launcher.Core.Assets;

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
    
    public async Task<string> GetOrDownloadAssetAsync(string url, string assetId)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(assetId)) return null;
        
        string localPath = Path.Combine(_cacheDirectory, $"{assetId}.png");
        
        if (File.Exists(localPath))
        {
            return localPath;
        }

        try
        {
            byte[] imageBytes = await _httpClient.GetByteArrayAsync(url);
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