namespace Launcher.Infrastructure.Updates.Models;

public record UpdateCheckResult(
    bool IsUpdateAvailable,
    string CurrentVersion,
    AppReleaseInfo? TargetRelease);