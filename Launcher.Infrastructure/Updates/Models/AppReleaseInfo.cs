namespace Launcher.Infrastructure.Updates.Models;

public record AppReleaseInfo(
    string Version,
    string Title,
    string? Changelog,
    DateTimeOffset PublishedAt,
    bool IsPrerelease,
    string DownloadUrl,
    string FileName,
    long FileSizeBytes,
    string Sha256,
    bool IsDelta);