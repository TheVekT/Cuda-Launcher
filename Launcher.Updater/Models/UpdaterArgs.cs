namespace Launcher.Updater.Models;

public record UpdaterArgs(
    int? ProcessId,
    string TargetDirectory,
    string TargetVersion,
    string RestartExecutablePath,
    string DownloadUrl,
    string ExpectedSha256);