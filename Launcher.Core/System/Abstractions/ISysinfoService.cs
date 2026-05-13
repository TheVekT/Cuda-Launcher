namespace Launcher.Core.System.Abstractions;

public interface ISysInfoService
{
    long GetTotalRAMInMB();
    IEnumerable<string> GetPrimaryMonitorResolutions();
}