using System.Net.Http.Headers;
using System.Text.Json;
using FluentResults;
using Launcher.Infrastructure.Updates.Abstractions;
using Launcher.Infrastructure.Updates.Models;

namespace Launcher.Infrastructure.Updates;

public class GitHubUpdateCheckerService : IUpdateCheckerService
{
    private readonly HttpClient _httpClient;
    private readonly string _repositoryOwner;
    private readonly string _repositoryName;

    public GitHubUpdateCheckerService(
        HttpClient httpClient, 
        string repositoryOwner, 
        string repositoryName)
    {
        _httpClient = httpClient;
        _repositoryOwner = repositoryOwner;
        _repositoryName = repositoryName;
    }

    public async Task<Result<UpdateCheckResult>> CheckForUpdatesAsync(
        string currentVersion, 
        bool includePrereleases = false, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            string requestUri = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/releases";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MinecraftLauncher", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Fail<UpdateCheckResult>(
                    $"Failed to fetch releases from GitHub API. Status: {response.StatusCode}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return Result.Fail<UpdateCheckResult>("Malformed GitHub API response: expected JSON array.");
            }

            var candidateReleases = new List<AppReleaseInfo>();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                bool isDraft = element.TryGetProperty("draft", out var draftProp) && draftProp.GetBoolean();
                if (isDraft) continue;

                bool isPrerelease = element.TryGetProperty("prerelease", out var preProp) && preProp.GetBoolean();
                if (isPrerelease && !includePrereleases) continue;

                string rawTag = element.GetProperty("tag_name").GetString() ?? string.Empty;
                string cleanVersion = NormalizeVersionString(rawTag);
                if (string.IsNullOrWhiteSpace(cleanVersion)) continue;

                string title = element.TryGetProperty("name", out var nameProp) && !string.IsNullOrWhiteSpace(nameProp.GetString())
                    ? nameProp.GetString()!
                    : rawTag;

                string? changelog = element.TryGetProperty("body", out var bodyProp)
                    ? bodyProp.GetString()
                    : null;

                DateTimeOffset publishedAt = element.TryGetProperty("published_at", out var pubProp) && pubProp.TryGetDateTimeOffset(out var pubDate)
                    ? pubDate
                    : DateTimeOffset.UtcNow;

                if (element.TryGetProperty("assets", out var assetsProp) && assetsProp.ValueKind == JsonValueKind.Array)
                {
                    var targetAsset = SelectTargetAsset(assetsProp);
                    if (targetAsset != null)
                    {
                        candidateReleases.Add(new AppReleaseInfo(
                            cleanVersion,
                            title,
                            changelog,
                            publishedAt,
                            isPrerelease,
                            targetAsset.Value.DownloadUrl,
                            targetAsset.Value.FileName,
                            targetAsset.Value.Size));
                    }
                }
            }

            if (candidateReleases.Count == 0)
            {
                return Result.Ok(new UpdateCheckResult(false, currentVersion, null));
            }

            var latestRelease = candidateReleases
                .OrderByDescending(r => r.Version, new SemVersionComparer())
                .FirstOrDefault();

            if (latestRelease == null)
            {
                return Result.Ok(new UpdateCheckResult(false, currentVersion, null));
            }

            bool isNewer = CompareVersions(latestRelease.Version, currentVersion) > 0;

            return Result.Ok(new UpdateCheckResult(isNewer, currentVersion, isNewer ? latestRelease : null));
        }
        catch (Exception ex)
        {
            return Result.Fail<UpdateCheckResult>(new Error($"Error checking updates: {ex.Message}").CausedBy(ex));
        }
    }

    private static (string DownloadUrl, string FileName, long Size)? SelectTargetAsset(JsonElement assetsArray)
    {
        var validAssets = new List<(string DownloadUrl, string FileName, long Size)>();

        foreach (var asset in assetsArray.EnumerateArray())
        {
            string name = asset.GetProperty("name").GetString() ?? string.Empty;
            string downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? string.Empty;
            long size = asset.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt64() : 0;

            if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                validAssets.Add((downloadUrl, name, size));
            }
        }
        
        var portableAsset = validAssets.FirstOrDefault(a => 
            a.FileName.Contains("Portable", StringComparison.OrdinalIgnoreCase) && 
            !a.FileName.Contains("delta", StringComparison.OrdinalIgnoreCase));

        if (portableAsset != default)
            return portableAsset;
        
        var generalZipAsset = validAssets.FirstOrDefault(a => 
            !a.FileName.Contains("delta", StringComparison.OrdinalIgnoreCase));

        if (generalZipAsset != default)
            return generalZipAsset;

        return null;
    }

    private static string NormalizeVersionString(string rawTag)
    {
        string trimmed = rawTag.Trim();
        if (trimmed.StartsWith('v') || trimmed.StartsWith('V'))
        {
            trimmed = trimmed[1..];
        }
        return trimmed;
    }

    private static int CompareVersions(string versionA, string versionB)
    {
        return new SemVersionComparer().Compare(versionA, versionB);
    }

    private class SemVersionComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var (coreX, prereleaseX) = SplitVersion(x);
            var (coreY, prereleaseY) = SplitVersion(y);

            Version.TryParse(coreX, out var parsedX);
            Version.TryParse(coreY, out var parsedY);

            parsedX ??= new Version(0, 0, 0);
            parsedY ??= new Version(0, 0, 0);

            int coreComparison = parsedX.CompareTo(parsedY);
            if (coreComparison != 0)
                return coreComparison;

            bool hasPreX = !string.IsNullOrWhiteSpace(prereleaseX);
            bool hasPreY = !string.IsNullOrWhiteSpace(prereleaseY);

            if (!hasPreX && hasPreY) return 1;
            if (hasPreX && !hasPreY) return -1;
            if (!hasPreX && !hasPreY) return 0;

            return string.Compare(prereleaseX, prereleaseY, StringComparison.OrdinalIgnoreCase);
        }

        private static (string Core, string? Prerelease) SplitVersion(string version)
        {
            int dashIndex = version.IndexOf('-');
            if (dashIndex > 0)
            {
                return (version[..dashIndex], version[(dashIndex + 1)..]);
            }
            return (version, null);
        }
    }
}