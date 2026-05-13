using Launcher.Core.Assets.Abstractions;
using Launcher.Core.Config.Abstractions;

namespace Launcher.Core.Assets;

public class IconsService : IIconsService
{
    private readonly string _iconsDirectory;
    private readonly string[] _supportedExtensions = { ".png", ".jpg", ".jpeg", ".ico", ".gif" };
    private readonly string[] _ignoredIcons = { "..." };

    public IconsService(ILauncherPathsService pathsService)
    {
        _iconsDirectory = Path.Combine(pathsService.AssetsDirectory, "Icons");
        if (!Directory.Exists(_iconsDirectory)) Directory.CreateDirectory(_iconsDirectory);
    }

    public List<string> GetAvailableIcons()
    {
        var iconList = new List<string>();
        var files = Directory.EnumerateFiles(_iconsDirectory)
            .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (!_ignoredIcons.Contains(fileName)) iconList.Add(file);
        }
        return iconList;
    }

    public string? ImportIcon(string sourceFilePath)
    {
        try
        {
            var destPath = Path.Combine(_iconsDirectory, Path.GetFileName(sourceFilePath));
            File.Copy(sourceFilePath, destPath, true);
            return destPath;
        }
        catch { return null; }
    }

    public string GetIconName(string fullPath) => Path.GetFileName(fullPath);
}