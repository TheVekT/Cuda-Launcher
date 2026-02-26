namespace Launcher.Core.Models;

public class GlobalLaunchSettings
{
    public int MaxRamMb { get; set; }
    public bool IsFullscreen { get; set; }
    public string Resolution { get; set; } // Например, "1920x1080" или "Auto"
}