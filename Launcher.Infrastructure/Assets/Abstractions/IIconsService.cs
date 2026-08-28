namespace Launcher.Infrastructure.Assets.Abstractions;

public interface IIconsService
{
    List<string> GetAvailableIcons();
    string? ImportIcon(string sourceFilePath);
    string GetIconName(string fullPath);
}
