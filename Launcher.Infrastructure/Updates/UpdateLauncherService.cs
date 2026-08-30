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
        if (!updateResult.IsUpdateAvailable || updateResult.TargetRelease == null)
        {
            return Result.Fail("No update available to launch updater.");
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
            string targetVersion = updateResult.TargetRelease.Version;
            string restartExecutablePath = Path.Combine(appDirectory, MainExecutableName);

            string arguments = $"--pid {currentPid} --target \"{targetAppDirectory}\" --target-version \"{targetVersion}\" --restart \"{restartExecutablePath}\" --url \"{updateResult.TargetRelease.DownloadUrl}\"";

            var startInfo = new ProcessStartInfo
            {
                FileName = stagedUpdaterPath,
                Arguments = arguments,
                WorkingDirectory = tempDirectory,
                UseShellExecute = true,
                CreateNoWindow = false
            };

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
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error($"Failed to cleanup temp directory: {ex.Message}").CausedBy(ex));
        }
    }
}