using Launcher.Core.Instances.Models;

namespace Launcher.Core.Instances.Abstractions;

public interface IInstanceService
{
    void SaveInstances(IEnumerable<MinecraftInstance> instances);
    List<MinecraftInstance> LoadInstances();
    void DeleteInstance(string instanceId);
}