namespace Launcher.Core.Assets.Abstractions;

public interface IIconsService
{
    List<string> GetAvailableIcons();
    string? ImportIcon(string sourceFilePath);
    string GetIconName(string fullPath);
}
