using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Launcher.Updater.Models;
using Launcher.Updater.Services.Abstractions;

namespace Launcher.Updater.Services;

public class UpdateExecutionService : IUpdateExecutionService
{
    private readonly HttpClient _httpClient;
    private const string UpdateZipFileName = "update.zip";

    public UpdateExecutionService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> ApplyUpdateAsync(
        UpdaterArgs args, 
        IProgress<UpdateProgressInfo>? progress = null, 
        CancellationToken cancellationToken = default)
    {
        string localZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, UpdateZipFileName);

        try
        {
            if (args.ProcessId.HasValue && args.ProcessId.Value > 0)
            {
                await WaitForTargetProcessExitAsync(args.ProcessId.Value, progress, cancellationToken);
            }
            
            (string DownloadUrl, long Size)? assetInfo = null;

            if (!string.IsNullOrWhiteSpace(args.DownloadUrl))
            {
                assetInfo = (args.DownloadUrl, 0);
            }
            else
            {
                progress?.Report(new UpdateProgressInfo("Checking release information...", args.TargetVersion, null, 0));
                
                assetInfo = await FetchReleaseAssetInfoAsync(
                    args.RepositoryOwner, 
                    args.RepositoryName, 
                    args.TargetVersion, 
                    cancellationToken);
            }

            if (assetInfo == null)
            {
                progress?.Report(new UpdateProgressInfo("Update failed", "Target asset not found on GitHub", null, 0));
                return false;
            }
            
            bool downloadSuccess = await DownloadArchiveAsync(
                assetInfo.Value.DownloadUrl, 
                assetInfo.Value.Size, 
                localZipPath, 
                progress, 
                cancellationToken);

            if (!downloadSuccess)
            {
                return false;
            }
            
            bool extractSuccess = await ExtractArchiveAsync(
                localZipPath, 
                args.TargetDirectory, 
                progress, 
                cancellationToken);

            if (!extractSuccess)
            {
                return false;
            }
            
            if (File.Exists(localZipPath))
            {
                try
                {
                    File.Delete(localZipPath);
                }
                catch { /* ignore cleanup errors */ }
            }
            
            progress?.Report(new UpdateProgressInfo("Restarting application...", "Launching main process", null, 100));
            RestartApplication(args.RestartExecutablePath, args.TargetDirectory);

            return true;
        }
        catch (Exception ex)
        {
            progress?.Report(new UpdateProgressInfo("Error occurred", ex.Message, null, 0));
            return false;
        }
    }

    private static async Task WaitForTargetProcessExitAsync(
        int processId, 
        IProgress<UpdateProgressInfo>? progress, 
        CancellationToken cancellationToken)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            if (!process.HasExited)
            {
                progress?.Report(new UpdateProgressInfo("Waiting for launcher to close...", $"PID: {processId}", null, 0));
                await process.WaitForExitAsync(cancellationToken);
            }
        }
        catch (ArgumentException)
        {
            // Process has already exited.
        }

        // Give Windows a brief moment to fully release all file locks
        await Task.Delay(500, cancellationToken);
    }

    private async Task<(string DownloadUrl, long Size)?> FetchReleaseAssetInfoAsync(
        string owner, 
        string repo, 
        string targetVersion, 
        CancellationToken cancellationToken)
    {
        string requestUri = $"https://api.github.com/repos/{owner}/{repo}/releases";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CudaLauncherUpdater", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return null;

        foreach (var release in doc.RootElement.EnumerateArray())
        {
            string tagName = release.GetProperty("tag_name").GetString() ?? string.Empty;
            string cleanTag = tagName.TrimStart('v', 'V');

            if (string.Equals(cleanTag, targetVersion.TrimStart('v', 'V'), StringComparison.OrdinalIgnoreCase))
            {
                if (release.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        string name = asset.GetProperty("name").GetString() ?? string.Empty;
                        if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                            !name.Contains("delta", StringComparison.OrdinalIgnoreCase))
                        {
                            string downloadUrl = asset.GetProperty("browser_download_url").GetString() ?? string.Empty;
                            long size = asset.TryGetProperty("size", out var sizeProp) ? sizeProp.GetInt64() : 0;
                            return (downloadUrl, size);
                        }
                    }
                }
            }
        }

        return null;
    }

    private async Task<bool> DownloadArchiveAsync(
        string downloadUrl, 
        long totalBytes, 
        string destinationZipPath, 
        IProgress<UpdateProgressInfo>? progress, 
        CancellationToken cancellationToken)
    {
        progress?.Report(new UpdateProgressInfo("Downloading update...", "Starting download...", "0.0 MB/s", 0));

        using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return false;

        long effectiveTotalBytes = totalBytes > 0 
            ? totalBytes 
            : response.Content.Headers.ContentLength ?? 0;

        string initialRemaining = effectiveTotalBytes > 0 ? $" • {FormatBytes(effectiveTotalBytes)} left" : string.Empty;
        progress?.Report(new UpdateProgressInfo("Downloading update...", "Starting download...", $"0.0 MB/s{initialRemaining}", 0));

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            destinationZipPath, 
            FileMode.Create, 
            FileAccess.Write, 
            FileShare.None, 
            81920, 
            useAsync: true);

        byte[] buffer = new byte[81920];
        long totalBytesRead = 0;
        int bytesRead;

        var stopwatch = Stopwatch.StartNew();
        long lastSpeedBytes = 0;
        long lastSpeedTimeMs = 0;
        string speedText = "0.0 MB/s";

        while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalBytesRead += bytesRead;

            long elapsedMs = stopwatch.ElapsedMilliseconds;
            if (elapsedMs - lastSpeedTimeMs >= 500)
            {
                double elapsedSec = (elapsedMs - lastSpeedTimeMs) / 1000.0;
                double bytesPerSec = (totalBytesRead - lastSpeedBytes) / elapsedSec;
                speedText = FormatSpeed(bytesPerSec);

                lastSpeedBytes = totalBytesRead;
                lastSpeedTimeMs = elapsedMs;
            }

            double percent = effectiveTotalBytes > 0 
                ? Math.Min(100.0, (double)totalBytesRead / effectiveTotalBytes * 100.0) 
                : 0;

            string details = effectiveTotalBytes > 0 
                ? $"{FormatBytes(totalBytesRead)} / {FormatBytes(effectiveTotalBytes)}" 
                : FormatBytes(totalBytesRead);

            string speedInfo;
            if (effectiveTotalBytes > 0)
            {
                long remainingBytes = Math.Max(0, effectiveTotalBytes - totalBytesRead);
                speedInfo = $"{speedText} • {FormatBytes(remainingBytes)} left";
            }
            else
            {
                speedInfo = speedText;
            }

            progress?.Report(new UpdateProgressInfo("Downloading update...", details, speedInfo, percent));
        }

        progress?.Report(new UpdateProgressInfo("Downloading update...", "Download complete", "0.0 MB/s • 0.0 MB left", 100));
        return true;
    }

    private static async Task<bool> ExtractArchiveAsync(
        string zipPath, 
        string targetDirectory, 
        IProgress<UpdateProgressInfo>? progress, 
        CancellationToken cancellationToken)
    {
        progress?.Report(new UpdateProgressInfo("Extracting update...", "Preparing files...", null, 0));

        return await Task.Run(() =>
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                int totalEntries = archive.Entries.Count;
                int currentEntry = 0;
                string fullTargetDirectory = Path.GetFullPath(targetDirectory);

                // Detect if all files in the zip are wrapped in a single root directory (e.g. "Release/..." or "CudaLauncher/...")
                string? commonRoot = null;
                var exeEntry = archive.Entries.FirstOrDefault(e => e.Name.Equals("Cuda Launcher.exe", StringComparison.OrdinalIgnoreCase));
                if (exeEntry != null)
                {
                    int slashIndex = exeEntry.FullName.IndexOfAny(new[] { '/', '\\' });
                    if (slashIndex > 0)
                    {
                        string candidateRoot = exeEntry.FullName[..(slashIndex + 1)];
                        if (archive.Entries.All(e => e.FullName.StartsWith(candidateRoot, StringComparison.OrdinalIgnoreCase)))
                        {
                            commonRoot = candidateRoot;
                        }
                    }
                }

                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string relativePath = entry.FullName;
                    if (!string.IsNullOrEmpty(commonRoot) && relativePath.StartsWith(commonRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        relativePath = relativePath[commonRoot.Length..];
                    }

                    if (string.IsNullOrWhiteSpace(relativePath)) continue;

                    // Never overwrite the running temp folder or updater inside temp
                    if (relativePath.StartsWith("temp/", StringComparison.OrdinalIgnoreCase) ||
                        relativePath.StartsWith("temp\\", StringComparison.OrdinalIgnoreCase))
                    {
                        currentEntry++;
                        continue;
                    }

                    string destinationPath = Path.GetFullPath(Path.Combine(fullTargetDirectory, relativePath));
                    
                    if (!destinationPath.StartsWith(fullTargetDirectory, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destinationPath);
                        currentEntry++;
                        continue;
                    }

                    string? parentDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(parentDir))
                    {
                        Directory.CreateDirectory(parentDir);
                    }

                    int retries = 0;
                    while (true)
                    {
                        try
                        {
                            entry.ExtractToFile(destinationPath, overwrite: true);
                            break;
                        }
                        catch (IOException) when (++retries <= 5)
                        {
                            Thread.Sleep(200);
                        }
                    }

                    currentEntry++;

                    double percent = totalEntries > 0 
                        ? (double)currentEntry / totalEntries * 100.0 
                        : 0;

                    progress?.Report(new UpdateProgressInfo("Extracting update...", entry.Name, null, percent));
                }

                progress?.Report(new UpdateProgressInfo("Extracting update...", "Extraction complete", null, 100));
                return true;
            }
            catch (Exception ex)
            {
                progress?.Report(new UpdateProgressInfo("Extraction failed", ex.Message, null, 0));
                return false;
            }
        }, cancellationToken);
    }

    private static void RestartApplication(string executablePath, string targetDirectory)
    {
        string? targetExe = executablePath;
        if (string.IsNullOrEmpty(targetExe) || !File.Exists(targetExe))
        {
            string fallback = Path.Combine(targetDirectory, "Cuda Launcher.exe");
            if (File.Exists(fallback))
            {
                targetExe = fallback;
            }
        }

        if (string.IsNullOrEmpty(targetExe) || !File.Exists(targetExe)) return;

        var startInfo = new ProcessStartInfo
        {
            FileName = targetExe,
            WorkingDirectory = string.IsNullOrEmpty(targetDirectory) ? Path.GetDirectoryName(targetExe)! : targetDirectory,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    private static string FormatBytes(long bytes)
    {
        return $"{(double)bytes / (1024 * 1024):F1} MB";
    }

    private static string FormatSpeed(double bytesPerSec)
    {
        return $"{(bytesPerSec / (1024 * 1024)):F1} MB/s";
    }
}