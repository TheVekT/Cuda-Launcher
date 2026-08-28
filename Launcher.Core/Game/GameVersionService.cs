using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using CmlLib.Core;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.VersionMetadata;
using Launcher.Core.Common.Enums;
using Launcher.Core.Config.Abstractions;
using Launcher.Core.Game.Abstractions;
using Launcher.Core.System.Abstractions;

namespace Launcher.Core.Game;

public class GameVersionService : IGameVersionService
{
    private static readonly string[] PreReleaseKeywords = ["beta", "alpha", "rc", "snapshot"];

    private readonly IConnectivityService _connectivityService;
    private readonly MinecraftLauncher _launcher;
    private readonly HttpClient _httpClient;
    private readonly string _versionsPath;

    private IReadOnlyList<IVersionMetadata>? _cachedAllMetadata;
    private readonly Dictionary<GameLoaderType, HashSet<string>> _cachedSupportedVersions = new();

    public GameVersionService(
        ILauncherPathsService pathsService, 
        HttpClient httpClient, 
        IConnectivityService connectivityService)
    {
        _httpClient = httpClient;
        _connectivityService = connectivityService;

        string globalMinecraftPath = Path.Combine(pathsService.DataDirectory, "Global");
        var path = new MinecraftPath(globalMinecraftPath);
        _launcher = new MinecraftLauncher(path);
        _versionsPath = Path.Combine(globalMinecraftPath, "versions");
    }

    private Task<bool> CheckOnlineAsync() => _connectivityService.CheckInternetAccessAsync();

    public async Task<IEnumerable<string>> GetVanillaVersionsAsync(bool showSnapshots = false)
    {
        if (!await CheckOnlineAsync())
        {
            return GetLocalVanillaVersions(showSnapshots);
        }

        try
        {
            if (_cachedAllMetadata == null)
            {
                var versions = await _launcher.GetAllVersionsAsync();
                _cachedAllMetadata = versions.ToList();
            }

            return _cachedAllMetadata
                .Where(v => v.GetVersionType() == MVersionType.Release ||
                           (showSnapshots && v.GetVersionType() == MVersionType.Snapshot))
                .Select(v => v.Name)
                .ToList();
        }
        catch (Exception ex)
        {
            bool offline = ex is HttpRequestException or SocketException or TaskCanceledException;
            Debug.WriteLine($"[VersionService {(offline ? "Warn" : "Error")}] Failed to fetch vanilla versions: {ex.Message}");
            return GetLocalVanillaVersions(showSnapshots);
        }
    }

    public async Task<IEnumerable<string>> GetGameVersionsByTypeAsync(GameLoaderType type, bool showSnapshots = false)
    {
        return type switch
        {
            GameLoaderType.Vanilla => await GetVanillaVersionsAsync(showSnapshots),
            GameLoaderType.Forge => await GetForgeSupportedMcVersions(showSnapshots),
            GameLoaderType.NeoForge => await GetNeoForgeSupportedMcVersions(showSnapshots),
            GameLoaderType.Fabric => await GetFabricOrQuiltSupportedMcVersions(GameLoaderType.Fabric, "https://meta.fabricmc.net/v2/versions/game", showSnapshots),
            GameLoaderType.Quilt => await GetFabricOrQuiltSupportedMcVersions(GameLoaderType.Quilt, "https://meta.quiltmc.org/v3/versions/game", showSnapshots),
            _ => await GetVanillaVersionsAsync(showSnapshots)
        };
    }

    public async Task<IEnumerable<string>> GetLoaderVersionsAsync(GameLoaderType type, string gameVersion)
    {
        if (string.IsNullOrWhiteSpace(gameVersion))
            return [];

        if (!await CheckOnlineAsync())
        {
            return GetLocalLoaderVersions(type, gameVersion);
        }

        try
        {
            var versions = type switch
            {
                GameLoaderType.Vanilla => new List<string> { gameVersion },
                GameLoaderType.Forge => await GetForgeVersions(gameVersion),
                GameLoaderType.NeoForge => await GetNeoForgeVersions(gameVersion),
                GameLoaderType.Fabric => await GetFabricOrQuiltLoaderVersions("https://meta.fabricmc.net/v2/versions/loader/", GameLoaderType.Fabric, gameVersion),
                GameLoaderType.Quilt => await GetFabricOrQuiltLoaderVersions("https://meta.quiltmc.org/v3/versions/loader/", GameLoaderType.Quilt, gameVersion),
                _ => []
            };

            return SortVersionsDescending(versions);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VersionService Error] Failed to fetch loader versions for {type}: {ex.Message}");
            return GetLocalLoaderVersions(type, gameVersion);
        }
    }

    public async Task<string?> GetRecommendedLoaderVersionAsync(GameLoaderType type, string gameVersion)
    {
        if (string.IsNullOrWhiteSpace(gameVersion)) return null;

        try
        {
            var versions = (await GetLoaderVersionsAsync(type, gameVersion)).ToList();
            if (!versions.Any()) return null;

            if (type == GameLoaderType.Vanilla)
                return gameVersion;

            var stableVersion = versions.FirstOrDefault(v =>
                !PreReleaseKeywords.Any(k => v.Contains(k, StringComparison.OrdinalIgnoreCase)));

            return stableVersion ?? versions.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VersionService Error] Failed to calculate recommended version for {type}: {ex.Message}");
            return null;
        }
    }

    // --- LOADER VERSION FETCHERS ---

    private async Task<IEnumerable<string>> GetSupportedMcVersionsAsync(
        GameLoaderType type,
        bool showSnapshots,
        Func<Task<IEnumerable<string>>> fetchOnline)
    {
        if (!await CheckOnlineAsync())
        {
            return GetLocalInstalledMcVersionsForLoader(type);
        }

        if (_cachedSupportedVersions.TryGetValue(type, out var cached))
            return FilterWithVanilla(await GetVanillaVersionsAsync(showSnapshots), cached);

        try
        {
            var supported = await fetchOnline();
            var set = new HashSet<string>(supported, StringComparer.OrdinalIgnoreCase);
            _cachedSupportedVersions[type] = set;
            return FilterWithVanilla(await GetVanillaVersionsAsync(showSnapshots), set);
        }
        catch (Exception ex)
        {
            bool offline = ex is HttpRequestException or SocketException or TaskCanceledException;
            Debug.WriteLine($"[VersionService {(offline ? "Warn" : "Error")}] Failed to fetch supported versions for {type}: {ex.Message}");
            return GetLocalInstalledMcVersionsForLoader(type);
        }
    }

    private Task<IEnumerable<string>> GetForgeSupportedMcVersions(bool showSnapshots) =>
        GetSupportedMcVersionsAsync(GameLoaderType.Forge, showSnapshots, async () =>
        {
            using var doc = JsonDocument.Parse(await _httpClient.GetStringAsync("https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json"));
            return doc.RootElement.GetProperty("promos").EnumerateObject()
                .Select(p => p.Name)
                .Where(k => k.Contains('-'))
                .Select(k => k.Substring(0, k.IndexOf('-')))
                .ToList();
        });

    private Task<IEnumerable<string>> GetNeoForgeSupportedMcVersions(bool showSnapshots) =>
        GetSupportedMcVersionsAsync(GameLoaderType.NeoForge, showSnapshots, async () =>
        {
            using var doc = JsonDocument.Parse(await _httpClient.GetStringAsync("https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge"));
            return doc.RootElement.GetProperty("versions").EnumerateArray()
                .Select(v => v.GetString())
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => v!.StartsWith("1.") && v.Contains('-')
                    ? v.Substring(0, v.IndexOf('-'))
                    : Version.TryParse(v.Split('-')[0], out var ver) ? (ver.Minor == 0 ? $"1.{ver.Major}" : $"1.{ver.Major}.{ver.Minor}") : null)
                .Where(v => v != null)
                .Select(v => v!)
                .ToList();
        });

    private Task<IEnumerable<string>> GetFabricOrQuiltSupportedMcVersions(GameLoaderType type, string url, bool showSnapshots) =>
        GetSupportedMcVersionsAsync(type, showSnapshots, async () =>
        {
            using var doc = JsonDocument.Parse(await _httpClient.GetStringAsync(url));
            return doc.RootElement.EnumerateArray()
                .Where(e => showSnapshots || (e.TryGetProperty("stable", out var s) && s.GetBoolean()))
                .Select(e => e.GetProperty("version").GetString())
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => v!)
                .ToList();
        });

    private async Task<IEnumerable<string>> GetForgeVersions(string gameVersion)
    {
        try
        {
            var versions = await new ForgeInstaller(_launcher).GetForgeVersions(gameVersion);
            return versions.Select(v => v.ForgeVersionName).ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VersionService Error] Failed to fetch Forge versions: {ex.Message}");
            return GetLocalLoaderVersions(GameLoaderType.Forge, gameVersion);
        }
    }

    private async Task<IEnumerable<string>> GetNeoForgeVersions(string gameVersion)
    {
        try
        {
            var versions = await new NeoForgeInstaller(_launcher).GetForgeVersions(gameVersion);
            return versions.Select(v => v.VersionName).ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VersionService Error] Failed to fetch NeoForge versions: {ex.Message}");
            return GetLocalLoaderVersions(GameLoaderType.NeoForge, gameVersion);
        }
    }

    private async Task<IEnumerable<string>> GetFabricOrQuiltLoaderVersions(string baseUrl, GameLoaderType type, string gameVersion)
    {
        try
        {
            using var doc = JsonDocument.Parse(await _httpClient.GetStringAsync($"{baseUrl}{gameVersion}"));
            return doc.RootElement.EnumerateArray()
                .Select(e => e.GetProperty("loader").GetProperty("version").GetString())
                .Where(v => !string.IsNullOrEmpty(v))
                .Select(v => v!)
                .ToList();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[VersionService Error] Failed to fetch {type} loader versions: {ex.Message}");
            return GetLocalLoaderVersions(type, gameVersion);
        }
    }

    // --- LOCAL FALLBACK PARSERS ---

    private record struct LocalVersionInfo(
        string Id,
        GameLoaderType LoaderType,
        string McVersion,
        string? LoaderVersion,
        string? Type,
        // ReSharper disable once NotAccessedPositionalProperty.Local
        DateTimeOffset? ReleaseTime);

    private LocalVersionInfo? ParseLocalVersion(string dirPath)
    {
        string id = Path.GetFileName(dirPath);
        if (!TryGetJsonMetadata(dirPath, out string? inheritsFrom, out string? typeStr, out DateTimeOffset? releaseTime))
            return null;

        // Fabric / Quilt
        bool isFabric = id.StartsWith("fabric-loader-", StringComparison.OrdinalIgnoreCase);
        bool isQuilt = id.StartsWith("quilt-loader-", StringComparison.OrdinalIgnoreCase);
        if (isFabric || isQuilt)
        {
            var loaderType = isFabric ? GameLoaderType.Fabric : GameLoaderType.Quilt;
            string prefix = isFabric ? "fabric-loader-" : "quilt-loader-";
            string withoutPrefix = id.Substring(prefix.Length);
            int lastDash = withoutPrefix.LastIndexOf('-');
            string loaderVer = lastDash > 0 ? withoutPrefix.Substring(0, lastDash) : withoutPrefix;
            string mcVer = inheritsFrom ?? (lastDash > 0 ? withoutPrefix.Substring(lastDash + 1) : "");
            return new LocalVersionInfo(id, loaderType, mcVer, loaderVer, typeStr, releaseTime);
        }

        // NeoForge
        if (id.StartsWith("neoforge-", StringComparison.OrdinalIgnoreCase) || id.Contains("-neoforge-", StringComparison.OrdinalIgnoreCase))
        {
            string loaderVer = id.StartsWith("neoforge-", StringComparison.OrdinalIgnoreCase)
                ? id.Substring("neoforge-".Length)
                : id.Substring(id.IndexOf("-neoforge-", StringComparison.OrdinalIgnoreCase) + "-neoforge-".Length);

            string mcVer = inheritsFrom ?? "";
            if (string.IsNullOrEmpty(mcVer) && Version.TryParse(loaderVer.Split('-')[0], out var v))
                mcVer = v.Minor == 0 ? $"1.{v.Major}" : $"1.{v.Major}.{v.Minor}";

            return new LocalVersionInfo(id, GameLoaderType.NeoForge, mcVer, loaderVer, typeStr, releaseTime);
        }

        // Forge
        if (id.Contains("-forge", StringComparison.OrdinalIgnoreCase))
        {
            int forgeIdx = id.IndexOf("-forge-", StringComparison.OrdinalIgnoreCase);
            string loaderVer = forgeIdx >= 0
                ? id.Substring(forgeIdx + "-forge-".Length)
                : id.Substring(id.IndexOf("-forge", StringComparison.OrdinalIgnoreCase) + "-forge".Length).TrimStart('-');

            string mcVer = inheritsFrom ?? (forgeIdx > 0 ? id.Substring(0, forgeIdx) : "");
            return new LocalVersionInfo(id, GameLoaderType.Forge, mcVer, loaderVer, typeStr, releaseTime);
        }

        // Vanilla
        if (string.IsNullOrEmpty(inheritsFrom))
        {
            return new LocalVersionInfo(id, GameLoaderType.Vanilla, id, null, typeStr, releaseTime);
        }

        return null;
    }

    /// <summary>
    /// Shared local-version scan: enumerates version directories, parses each one,
    /// filters/selects via the given delegates, and returns them sorted descending.
    /// </summary>
    private IEnumerable<string> GetLocalVersions(
        Func<LocalVersionInfo, bool> predicate,
        Func<LocalVersionInfo, string> selector)
    {
        if (!Directory.Exists(_versionsPath)) return [];

        return SortVersionsDescending(Directory.GetDirectories(_versionsPath)
            .Select(ParseLocalVersion)
            .Where(v => v.HasValue && predicate(v.Value))
            .Select(v => selector(v!.Value)));
    }

    private IEnumerable<string> GetLocalVanillaVersions(bool showSnapshots) =>
        GetLocalVersions(
            v => v.LoaderType == GameLoaderType.Vanilla && (showSnapshots || !IsSnapshot(v)),
            v => v.McVersion);

    private IEnumerable<string> GetLocalLoaderVersions(GameLoaderType type, string gameVersion) =>
        GetLocalVersions(
            v => v.LoaderType == type &&
                 string.Equals(v.McVersion, gameVersion, StringComparison.OrdinalIgnoreCase) &&
                 !string.IsNullOrEmpty(v.LoaderVersion),
            v => v.LoaderVersion!);

    private IEnumerable<string> GetLocalInstalledMcVersionsForLoader(GameLoaderType type) =>
        GetLocalVersions(
            v => v.LoaderType == type && !string.IsNullOrEmpty(v.McVersion),
            v => v.McVersion);

    private static bool IsSnapshot(LocalVersionInfo v)
    {
        return string.Equals(v.Type, "snapshot", StringComparison.OrdinalIgnoreCase) ||
               v.Id.Contains('w', StringComparison.OrdinalIgnoreCase) ||
               v.Id.Contains("-pre", StringComparison.OrdinalIgnoreCase) ||
               v.Id.Contains("-rc", StringComparison.OrdinalIgnoreCase);
    }

    private IEnumerable<string> SortVersionsDescending(IEnumerable<string> versions)
    {
        return versions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(v => GetVersionReleaseTime(v) ?? DateTimeOffset.MinValue)
            .ThenByDescending(v => v, new SemanticVersionComparer())
            .ToList();
    }

    private DateTimeOffset? GetVersionReleaseTime(string versionName)
    {
        string dirPath = Path.Combine(_versionsPath, versionName);
        return TryGetJsonMetadata(dirPath, out _, out _, out var releaseTime) ? releaseTime : null;
    }

    private static bool TryGetJsonMetadata(string dirPath, out string? inheritsFrom, out string? typeStr, out DateTimeOffset? releaseTime)
    {
        inheritsFrom = null;
        typeStr = null;
        releaseTime = null;

        try
        {
            string dirName = Path.GetFileName(dirPath);
            string jsonPath = Path.Combine(dirPath, $"{dirName}.json");
            if (!File.Exists(jsonPath)) return false;

            using var stream = File.OpenRead(jsonPath);
            using var doc = JsonDocument.Parse(stream);
            var root = doc.RootElement;

            if (root.TryGetProperty("inheritsFrom", out var inhProp))
                inheritsFrom = inhProp.GetString();

            if (root.TryGetProperty("type", out var typeProp))
                typeStr = typeProp.GetString();

            if (root.TryGetProperty("releaseTime", out var rtProp) && DateTimeOffset.TryParse(rtProp.GetString(), out var dt))
                releaseTime = dt;
            else if (root.TryGetProperty("time", out var tProp) && DateTimeOffset.TryParse(tProp.GetString(), out var dt2))
                releaseTime = dt2;

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static IEnumerable<string> FilterWithVanilla(IEnumerable<string> vanillaVersions, HashSet<string> supportedSet) =>
        vanillaVersions.Where(supportedSet.Contains).ToList();

    private class SemanticVersionComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (string.Equals(x, y, StringComparison.OrdinalIgnoreCase)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var partsX = x.Split(['.', '-', '+', '_'], StringSplitOptions.RemoveEmptyEntries);
            var partsY = y.Split(['.', '-', '+', '_'], StringSplitOptions.RemoveEmptyEntries);

            int maxLen = Math.Max(partsX.Length, partsY.Length);
            for (int i = 0; i < maxLen; i++)
            {
                string partX = i < partsX.Length ? partsX[i] : "0";
                string partY = i < partsY.Length ? partsY[i] : "0";

                bool isNumX = int.TryParse(partX, out int numX);
                bool isNumY = int.TryParse(partY, out int numY);

                if (isNumX && isNumY)
                {
                    if (numX != numY)
                        return numX.CompareTo(numY);
                }
                else
                {
                    int strComp = string.Compare(partX, partY, StringComparison.OrdinalIgnoreCase);
                    if (strComp != 0)
                        return strComp;
                }
            }

            return 0;
        }
    }
}