namespace Launcher.Core.Instances.Models;

public class GlobalLaunchSettings(string resolution, bool isFullscreen, int maxRamMb, string? jvmArguments = null)
{
    public int MaxRamMb { get; set; } = maxRamMb;
    public bool IsFullscreen { get; set; } = isFullscreen;
    public string Resolution { get; set; } = resolution;
    public string? JvmArguments { get; set; } = jvmArguments;
}