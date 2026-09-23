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

            var candidateReleases = new List<(JsonElement Element, string CleanVersion)>();

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                bool isDraft = element.TryGetProperty("draft", out var draftProp) && draftProp.GetBoolean();
                if (isDraft) continue;

                bool isPrerelease = element.TryGetProperty("prerelease", out var preProp) && preProp.GetBoolean();
                if (isPrerelease && !includePrereleases) continue;

                string rawTag = element.GetProperty("tag_name").GetString() ?? string.Empty;
                string cleanVersion = NormalizeVersionString(rawTag);
                if (string.IsNullOrWhiteSpace(cleanVersion)) continue;

                candidateReleases.Add((element.Clone(), cleanVersion));
            }

            if (candidateReleases.Count == 0)
            {
                return Result.Ok(new UpdateCheckResult(false, currentVersion, null));
            }

            var latestCandidate = candidateReleases
                .OrderByDescending(r => r.CleanVersion, new SemVersionComparer())
                .FirstOrDefault();

            string normalizedCurrentVersion = NormalizeVersionString(currentVersion);
            bool isNewer = CompareVersions(latestCandidate.CleanVersion, normalizedCurrentVersion) > 0;

            if (!isNewer)
            {
                return Result.Ok(new UpdateCheckResult(false, normalizedCurrentVersion, null));
            }

            var targetElement = latestCandidate.Element;

            string title = targetElement.TryGetProperty("name", out var nameProp) && !string.IsNullOrWhiteSpace(nameProp.GetString())
                ? nameProp.GetString()!
                : latestCandidate.CleanVersion;

            string? changelog = targetElement.TryGetProperty("body", out var bodyProp)
                ? bodyProp.GetString()
                : null;

            DateTimeOffset publishedAt = targetElement.TryGetProperty("published_at", out var pubProp) && pubProp.TryGetDateTimeOffset(out var pubDate)
                ? pubDate
                : DateTimeOffset.UtcNow;

            bool isReleasePrerelease = targetElement.TryGetProperty("prerelease", out var isPreProp) && isPreProp.GetBoolean();

            if (!targetElement.TryGetProperty("assets", out var assetsProp) || assetsProp.ValueKind != JsonValueKind.Array)
            {
                return Result.Fail<UpdateCheckResult>("No assets found in target release.");
            }

            string? manifestDownloadUrl = null;
            var availableAssets = new List<(string Name, string DownloadUrl, long Size)>();

            foreach (var asset in assetsProp.EnumerateArray())
            {
                string name = asset.GetProperty("name").GetString() ?? string.Empty;
                string downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? string.Empty;
                long size = asset.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt64() : 0;

                if (name.Equals("release.app.json", StringComparison.OrdinalIgnoreCase))
                {
                    manifestDownloadUrl = downloadUrl;
                }

                if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(downloadUrl))
                {
                    availableAssets.Add((name, downloadUrl, size));
                }
            }

            if (string.IsNullOrWhiteSpace(manifestDownloadUrl))
            {
                return Result.Fail<UpdateCheckResult>("Manifest 'release.app.json' not found in target release assets.");
            }

            ReleaseAppManifest? manifest;
            try
            {
                using var manifestRequest = new HttpRequestMessage(HttpMethod.Get, manifestDownloadUrl);
                manifestRequest.Headers.UserAgent.Add(new ProductInfoHeaderValue("MinecraftLauncher", "1.0"));

                using var manifestResponse = await _httpClient.SendAsync(manifestRequest, cancellationToken);
                if (!manifestResponse.IsSuccessStatusCode)
                {
                    return Result.Fail<UpdateCheckResult>(
                        $"Failed to download release.app.json. Status: {manifestResponse.StatusCode}");
                }

                await using var manifestStream = await manifestResponse.Content.ReadAsStreamAsync(cancellationToken);
                manifest = await JsonSerializer.DeserializeAsync<ReleaseAppManifest>(
                    manifestStream,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    cancellationToken);

                if (manifest == null)
                {
                    return Result.Fail<UpdateCheckResult>("Failed to deserialize release.app.json.");
                }
            }
            catch (Exception ex)
            {
                return Result.Fail<UpdateCheckResult>(new Error($"Error reading release.app.json: {ex.Message}").CausedBy(ex));
            }

            string targetFileName;
            string targetSha256;
            bool isDelta = false;

            bool canUseDelta = manifest.DeltaArchive != null &&
                               !string.IsNullOrWhiteSpace(manifest.DeltaArchive.BaseVersion) &&
                               NormalizeVersionString(manifest.DeltaArchive.BaseVersion)
                                   .Equals(normalizedCurrentVersion, StringComparison.OrdinalIgnoreCase);

            if (canUseDelta)
            {
                targetFileName = manifest.DeltaArchive!.FileName;
                targetSha256 = manifest.DeltaArchive.Sha256;
                isDelta = true;
            }
            else
            {
                targetFileName = manifest.FullArchive.FileName;
                targetSha256 = manifest.FullArchive.Sha256;
            }

            if (string.IsNullOrWhiteSpace(targetFileName) || string.IsNullOrWhiteSpace(targetSha256))
            {
                return Result.Fail<UpdateCheckResult>("Archive fileName or sha256 is missing in release.app.json.");
            }

            var chosenAsset = availableAssets.FirstOrDefault(a => 
                a.Name.Equals(targetFileName, StringComparison.OrdinalIgnoreCase));

            if (chosenAsset == default)
            {
                return Result.Fail<UpdateCheckResult>(
                    $"Archive file '{targetFileName}' specified in release.app.json was not found in release assets.");
            }

            var releaseInfo = new AppReleaseInfo(
                latestCandidate.CleanVersion,
                title,
                changelog,
                publishedAt,
                isReleasePrerelease,
                chosenAsset.DownloadUrl,
                chosenAsset.Name,
                chosenAsset.Size,
                targetSha256,
                isDelta);

            return Result.Ok(new UpdateCheckResult(true, normalizedCurrentVersion, releaseInfo));
        }
        catch (Exception ex)
        {
            return Result.Fail<UpdateCheckResult>(new Error($"Error checking updates: {ex.Message}").CausedBy(ex));
        }
    }

    private static string NormalizeVersionString(string rawTag)
    {
        if (string.IsNullOrWhiteSpace(rawTag))
            return string.Empty;

        return rawTag.Trim().TrimStart('v', 'V');
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
            string clean = NormalizeVersionString(version);
            int dashIndex = clean.IndexOf('-');
            if (dashIndex > 0)
            {
                return (clean[..dashIndex], clean[(dashIndex + 1)..]);
            }
            return (clean, null);
        }
    }
}