using System.Diagnostics;
using FluentResults;
using Launcher.Core.Config.Abstractions;
using Launcher.Infrastructure.Updates.Abstractions;
using Launcher.Infrastructure.Updates.Models;

namespace Launcher.Infrastructure.Updates;

public class UpdateLauncherService : IUpdateLauncherService
{
    private const string UpdaterExecutableName = "Launcher Updater.exe";
    private const string MainExecutableName = "Cuda Launcher.exe";
    private const string TempFolderName = "temp";

    private readonly ILauncherPathsService _pathsService;

    public UpdateLauncherService(ILauncherPathsService pathsService)
    {
        _pathsService = pathsService;
    }

    public Result LaunchUpdater(UpdateCheckResult updateResult)
    {
        var release = updateResult.TargetRelease;
        var invalidReasons = new List<string>();

        if (!updateResult.IsUpdateAvailable || release == null)
            invalidReasons.Add("Update is not available or TargetRelease is missing");
        if (string.IsNullOrWhiteSpace(release?.Version))
            invalidReasons.Add("TargetVersion is empty");
        if (string.IsNullOrWhiteSpace(release?.DownloadUrl))
            invalidReasons.Add("DownloadUrl is empty");
        if (string.IsNullOrWhiteSpace(release?.Sha256))
            invalidReasons.Add("Sha256 is empty");

        if (invalidReasons.Count > 0)
        {
            return Result.Fail($"Cannot launch updater: {string.Join(", ", invalidReasons)}.");
        }

        try
        {
            string appDirectory = _pathsService.BaseDirectory;
            string sourceUpdaterPath = Path.Combine(appDirectory, UpdaterExecutableName);

            if (!File.Exists(sourceUpdaterPath))
            {
                return Result.Fail($"Updater executable not found at: {sourceUpdaterPath}");
            }

            string tempDirectory = Path.Combine(appDirectory, TempFolderName);
            Directory.CreateDirectory(tempDirectory);

            string stagedUpdaterPath = Path.Combine(tempDirectory, UpdaterExecutableName);
            File.Copy(sourceUpdaterPath, stagedUpdaterPath, overwrite: true);

            int currentPid = Environment.ProcessId;
            string targetAppDirectory = appDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string targetVersion = release!.Version;
            string restartExecutablePath = Path.Combine(appDirectory, MainExecutableName);
            string downloadUrl = release.DownloadUrl;
            string archiveSha256 = release.Sha256;

            var startInfo = new ProcessStartInfo
            {
                FileName = stagedUpdaterPath,
                WorkingDirectory = tempDirectory,
                UseShellExecute = false,
                CreateNoWindow = false
            };
            startInfo.ArgumentList.Add("--pid");
            startInfo.ArgumentList.Add(currentPid.ToString());
            startInfo.ArgumentList.Add("--target");
            startInfo.ArgumentList.Add(targetAppDirectory);
            startInfo.ArgumentList.Add("--target-version");
            startInfo.ArgumentList.Add(targetVersion);
            startInfo.ArgumentList.Add("--restart");
            startInfo.ArgumentList.Add(restartExecutablePath);
            startInfo.ArgumentList.Add("--url");
            startInfo.ArgumentList.Add(downloadUrl);
            startInfo.ArgumentList.Add("--sha256");
            startInfo.ArgumentList.Add(archiveSha256);

            var process = Process.Start(startInfo);
            if (process == null)
            {
                return Result.Fail("Failed to start updater process.");
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error($"Failed to stage and launch updater: {ex.Message}").CausedBy(ex));
        }
    }

    public Result CleanupTempDirectory()
    {
        try
        {
            string tempDirectory = Path.Combine(_pathsService.BaseDirectory, TempFolderName);
            if (!Directory.Exists(tempDirectory))
            {
                return Result.Ok();
            }

            int retries = 0;
            while (true)
            {
                try
                {
                    if (Directory.Exists(tempDirectory))
                    {
                        Directory.Delete(tempDirectory, recursive: true);
                    }
                    break;
                }
                catch (Exception) when (++retries <= 4)
                {
                    Thread.Sleep(250);
                }
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error($"Failed to cleanup temp directory: {ex.Message}").CausedBy(ex));
        }
    }
}