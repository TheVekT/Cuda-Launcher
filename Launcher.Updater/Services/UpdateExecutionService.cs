using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
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
        var invalidArguments = new List<string>();

        if (string.IsNullOrWhiteSpace(args.TargetDirectory))
            invalidArguments.Add(nameof(args.TargetDirectory));
        if (string.IsNullOrWhiteSpace(args.TargetVersion))
            invalidArguments.Add(nameof(args.TargetVersion));
        if (string.IsNullOrWhiteSpace(args.RestartExecutablePath))
            invalidArguments.Add(nameof(args.RestartExecutablePath));
        if (string.IsNullOrWhiteSpace(args.DownloadUrl))
            invalidArguments.Add(nameof(args.DownloadUrl));
        if (string.IsNullOrWhiteSpace(args.ExpectedSha256))
            invalidArguments.Add(nameof(args.ExpectedSha256));

        if (invalidArguments.Count > 0)
        {
            progress?.Report(new UpdateProgressInfo(
                "Update failed", 
                $"Missing or invalid arguments: {string.Join(", ", invalidArguments)}", 
                null, 
                0));
            return false;
        }

        string localZipPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, UpdateZipFileName);

        try
        {
            if (args.ProcessId.HasValue && args.ProcessId.Value > 0)
            {
                await WaitForTargetProcessExitAsync(args.ProcessId.Value, progress, cancellationToken);
            }

            bool downloadSuccess = await DownloadArchiveAsync(
                args.DownloadUrl, 
                0L, 
                localZipPath, 
                progress, 
                cancellationToken);

            if (!downloadSuccess)
            {
                return false;
            }

            progress?.Report(new UpdateProgressInfo("Verifying update...", "Checking archive SHA-256...", null, 100));
            bool isHashValid = await VerifyFileSha256Async(localZipPath, args.ExpectedSha256, cancellationToken);
            if (!isHashValid)
            {
                progress?.Report(new UpdateProgressInfo("Verification failed", "SHA-256 checksum mismatch", null, 0));
                TryDeleteFile(localZipPath);
                return false;
            }

            bool applySuccess = await ApplyArchiveAndCleanupAsync(
                localZipPath, 
                args.TargetDirectory, 
                progress, 
                cancellationToken);

            if (!applySuccess)
            {
                return false;
            }

            TryDeleteFile(localZipPath);

            progress?.Report(new UpdateProgressInfo("Update complete", "All files updated successfully", null, 100));
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

        await Task.Delay(500, cancellationToken);
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

    private static async Task<bool> VerifyFileSha256Async(
        string filePath, 
        string expectedSha256, 
        CancellationToken cancellationToken)
    {
        if (!File.Exists(filePath))
            return false;

        using var sha256 = SHA256.Create();
        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        byte[] hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        string actualSha256 = Convert.ToHexString(hash);

        return string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<bool> ApplyArchiveAndCleanupAsync(
        string zipPath, 
        string targetDirectory, 
        IProgress<UpdateProgressInfo>? progress, 
        CancellationToken cancellationToken)
    {
        progress?.Report(new UpdateProgressInfo("Extracting update...", "Preparing files...", null, 0));

        return await Task.Run(async () =>
        {
            try
            {
                string fullTargetDirectory = Path.GetFullPath(targetDirectory);
                string targetDirectoryWithSeparator = fullTargetDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

                AppManifest? currentManifest = null;
                string localManifestPath = Path.Combine(fullTargetDirectory, "app-manifest.json");
                if (File.Exists(localManifestPath))
                {
                    try
                    {
                        string localJson = await File.ReadAllTextAsync(localManifestPath, cancellationToken);
                        currentManifest = JsonSerializer.Deserialize<AppManifest>(
                            localJson, 
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch
                    {
                        currentManifest = null;
                    }
                }

                using var archive = ZipFile.OpenRead(zipPath);

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

                string manifestEntryName = !string.IsNullOrEmpty(commonRoot) ? $"{commonRoot}app-manifest.json" : "app-manifest.json";
                var manifestEntry = archive.GetEntry(manifestEntryName) ?? 
                                    archive.Entries.FirstOrDefault(e => e.Name.Equals("app-manifest.json", StringComparison.OrdinalIgnoreCase));

                AppManifest? newManifest = null;
                if (manifestEntry != null)
                {
                    await using var manifestStream = manifestEntry.Open();
                    newManifest = await JsonSerializer.DeserializeAsync<AppManifest>(
                        manifestStream, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, 
                        cancellationToken);
                }

                var filesToDelete = new List<string>();
                if (currentManifest != null && newManifest != null)
                {
                    var newFileKeys = newManifest.GetFilePaths()
                        .Select(NormalizeRelativePath)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    foreach (var oldFile in currentManifest.GetFilePaths())
                    {
                        string normalized = NormalizeRelativePath(oldFile);
                        if (IsGuardedPath(normalized))
                            continue;

                        if (!newFileKeys.Contains(normalized))
                        {
                            filesToDelete.Add(normalized);
                        }
                    }
                }

                int totalEntries = archive.Entries.Count;
                int currentEntry = 0;

                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string relativePath = entry.FullName;
                    if (!string.IsNullOrEmpty(commonRoot) && relativePath.StartsWith(commonRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        relativePath = relativePath[commonRoot.Length..];
                    }

                    if (string.IsNullOrWhiteSpace(relativePath)) continue;

                    // Never overwrite the running temp folder or updater inside temp, or user Data directory
                    if (relativePath.StartsWith("temp/", StringComparison.OrdinalIgnoreCase) ||
                        relativePath.StartsWith("temp\\", StringComparison.OrdinalIgnoreCase) ||
                        relativePath.StartsWith("data/", StringComparison.OrdinalIgnoreCase) ||
                        relativePath.StartsWith("data\\", StringComparison.OrdinalIgnoreCase))
                    {
                        currentEntry++;
                        continue;
                    }

                    string destinationPath = Path.GetFullPath(Path.Combine(fullTargetDirectory, relativePath));

                    // Prevent Path Traversal (Zip Slip)
                    if (!destinationPath.StartsWith(targetDirectoryWithSeparator, StringComparison.OrdinalIgnoreCase))
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
                    double percent = totalEntries > 0 ? (double)currentEntry / totalEntries * 100.0 : 0;
                    progress?.Report(new UpdateProgressInfo("Extracting update...", entry.Name, null, percent));
                }

                if (filesToDelete.Count > 0)
                {
                    progress?.Report(new UpdateProgressInfo("Cleaning up...", "Removing obsolete files...", null, 100));
                    DeleteObsoleteFiles(fullTargetDirectory, filesToDelete);
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

    private static void DeleteObsoleteFiles(string fullTargetDirectory, IEnumerable<string> filesToDelete)
    {
        string targetDirectoryWithSeparator = fullTargetDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

        foreach (var relPath in filesToDelete)
        {
            try
            {
                if (IsGuardedPath(relPath))
                    continue;

                string fullPath = Path.GetFullPath(Path.Combine(fullTargetDirectory, relPath.Replace('/', Path.DirectorySeparatorChar)));
                // Prevent Path Traversal
                if (!fullPath.StartsWith(targetDirectoryWithSeparator, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                string? parentDir = Path.GetDirectoryName(fullPath);
                while (!string.IsNullOrEmpty(parentDir) &&
                       !parentDir.Equals(fullTargetDirectory, StringComparison.OrdinalIgnoreCase) &&
                       parentDir.StartsWith(targetDirectoryWithSeparator, StringComparison.OrdinalIgnoreCase))
                {
                    string relParent = Path.GetRelativePath(fullTargetDirectory, parentDir);
                    if (IsGuardedPath(relParent))
                        break;

                    if (Directory.Exists(parentDir) && !Directory.EnumerateFileSystemEntries(parentDir).Any())
                    {
                        Directory.Delete(parentDir);
                        parentDir = Path.GetDirectoryName(parentDir);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch
            {
                // ignore any exceptions during deletion, as it's not critical to the update process
            }
        }
    }

    private static bool IsGuardedPath(string relativePath)
    {
        string normalized = NormalizeRelativePath(relativePath);

        // Critical executable and manifest files in the root that must never be deleted
        if (normalized.Equals("app-manifest.json", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Cuda Launcher.exe", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Launcher Updater.exe", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Protected directories and everything inside them
        // Crucial: "Data" contains all user configs, accounts, Minecraft instances, and saves
        if (normalized.Equals("data", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("data/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("temp", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("temp/", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("cache", StringComparison.OrdinalIgnoreCase) ||
            normalized.StartsWith("cache/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeRelativePath(string path)
    {
        return path.Replace('\\', '/').Trim('/', ' ');
    }

    private static void TryDeleteFile(string filePath)
    {
        if (!File.Exists(filePath)) return;
        try
        {
            File.Delete(filePath);
        }
        catch
        {
            // ignore any exceptions during deletion, as it's not critical to the update process
        }
    }

    public void RestartApplication(string executablePath, string targetDirectory)
    {
        string targetExe = executablePath;
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