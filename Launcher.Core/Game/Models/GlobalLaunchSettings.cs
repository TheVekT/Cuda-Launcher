namespace Launcher.Core.Game.Models;

public class GlobalLaunchSettings(int allocatedMemory, bool fullscreen, string gameResolution, string? jvmArgs = null)
{
    public int AllocatedMemory { get; set; } = allocatedMemory;
    public string? JvmArgs { get; set; } = jvmArgs;
    public bool Fullscreen { get; set; } = fullscreen;
    public string GameResolution { get; set; } = gameResolution;
}