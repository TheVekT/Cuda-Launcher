using System.Net.Http.Headers;
using System.Text.Json;
using FluentResults;
using Launcher.Infrastructure.Updates.Abstractions;
using Launcher.Infrastructure.Updates.Models;

namespace Launcher.Infrastructure.Updates;

public class GitHubUpdateCheckerService : IUpdateCheckerService
{
    private const int StartFibonacciIndex = 4; // F(4) = 3
    private const int MaxFibonacciIndex = 13;   // F(13) = 233
    private static readonly TimeSpan InactivityResetThreshold = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly string _repositoryOwner;
    private readonly string _repositoryName;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private Result<UpdateCheckResult>? _cachedResult;
    private DateTimeOffset _lastCheckTime = DateTimeOffset.MinValue;
    private DateTimeOffset? _rateLimitResetTime;
    private int _consecutiveChecksCount;

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
        await _lock.WaitAsync(cancellationToken);
        try
        {
            var now = DateTimeOffset.UtcNow;

            // 1. Strict rate-limit lock: if all hourly requests were exhausted, lock network calls until reset
            if (_rateLimitResetTime.HasValue && now < _rateLimitResetTime.Value)
            {
                if (_cachedResult != null)
                    return _cachedResult;

                var waitSeconds = (int)Math.Ceiling((_rateLimitResetTime.Value - now).TotalSeconds);
                return Result.Fail<UpdateCheckResult>(
                    $"GitHub API rate limit exhausted. Try again in {waitSeconds} seconds.");
            }

            // 2. Inactivity threshold: reset progressive backoff if idle
            if (_lastCheckTime != DateTimeOffset.MinValue && (now - _lastCheckTime) > InactivityResetThreshold)
            {
                _consecutiveChecksCount = 0;
            }

            // 3. Fibonacci cooldown validation
            var activeCooldown = GetCooldownDuration(_consecutiveChecksCount);
            bool isCacheValid = _cachedResult != null && (now - _lastCheckTime) < activeCooldown;

            if (isCacheValid)
            {
                return _cachedResult!;
            }

            var result = await FetchReleasesFromGitHubAsync(currentVersion, includePrereleases, cancellationToken);

            if (result.IsSuccess)
            {
                _cachedResult = result;
                _lastCheckTime = now;
                _consecutiveChecksCount++;
            }
            else if (_rateLimitResetTime.HasValue && _cachedResult != null)
            {
                // Fallback to existing cache if the limit was reached on this call
                return _cachedResult;
            }

            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Result<UpdateCheckResult>> FetchReleasesFromGitHubAsync(
        string currentVersion, 
        bool includePrereleases, 
        CancellationToken cancellationToken)
    {
        try
        {
            string requestUri = $"https://api.github.com/repos/{_repositoryOwner}/{_repositoryName}/releases";

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("MinecraftLauncher", "1.0"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            
            // Read and store rate limit headers directly from the response
            UpdateRateLimitTracking(response.Headers);

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

    private void UpdateRateLimitTracking(HttpResponseHeaders headers)
    {
        if (headers.TryGetValues("x-ratelimit-remaining", out var remainingValues) &&
            int.TryParse(remainingValues.FirstOrDefault(), out var remaining))
        {
            if (headers.TryGetValues("x-ratelimit-reset", out var resetValues) &&
                long.TryParse(resetValues.FirstOrDefault(), out var resetUnixSeconds))
            {
                var resetTime = DateTimeOffset.FromUnixTimeSeconds(resetUnixSeconds);

                // If remaining requests count is exhausted, cache is locked strictly until reset time
                if (remaining <= 0)
                    _rateLimitResetTime = resetTime;
                else
                    _rateLimitResetTime = null;
            }
        }
    }

    private static TimeSpan GetCooldownDuration(int consecutiveChecks)
    {
        if (consecutiveChecks <= 0)
            return TimeSpan.Zero;

        int targetIndex = Math.Min(StartFibonacciIndex + (consecutiveChecks - 1), MaxFibonacciIndex);
        int seconds = CalculateFibonacci(targetIndex);

        return TimeSpan.FromSeconds(seconds);
    }

    private static int CalculateFibonacci(int n)
    {
        if (n <= 0) return 0;
        if (n == 1) return 1;

        int prev = 0;
        int current = 1;

        for (int i = 2; i <= n; i++)
        {
            int next = prev + current;
            prev = current;
            current = next;
        }

        return current;
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