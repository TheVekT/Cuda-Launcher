namespace Launcher.Updater.Models;

public record UpdateProgressInfo(
    string StatusTitle,
    string StatusDetails,
    string? DownloadSpeed,
    double Progress);