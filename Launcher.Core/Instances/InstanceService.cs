using System.Diagnostics;
using System.Text.Json;
using Launcher.Core.Config.Abstractions;
using Launcher.Core.Instances.Abstractions;
using Launcher.Core.Instances.Models;

namespace Launcher.Core.Instances;

public class InstanceService : IInstanceService
{
    private readonly string _instancesFolderPath;
    private readonly string _jsonFilePath;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly ILauncherPathsService _pathsService;

    public InstanceService(ILauncherPathsService pathsService)
    {
        _pathsService = pathsService;
        
        _instancesFolderPath = _pathsService.InstancesDirectory;
        _jsonFilePath = Path.Combine(_instancesFolderPath, "instances.json");
    }

    public void SaveInstances(IEnumerable<MinecraftInstance> instances)
    {
        if (!Directory.Exists(_instancesFolderPath))
        {
            Directory.CreateDirectory(_instancesFolderPath);
        }

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(instances, options);
            File.WriteAllText(_jsonFilePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving instances: {ex.Message}");
        }
    }

    public List<MinecraftInstance> LoadInstances()
    {
        if (!File.Exists(_jsonFilePath))
        {
            return new List<MinecraftInstance>();
        }

        try
        {
            var json = File.ReadAllText(_jsonFilePath);
            var instances = JsonSerializer.Deserialize<List<MinecraftInstance>>(json);
            return instances ?? new List<MinecraftInstance>();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Ошибка загрузки: {ex.Message}");
            return new List<MinecraftInstance>();
        }
    }

    public void DeleteInstance(string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId)) return;

        var currentInstances = LoadInstances();
        var instanceToRemove = currentInstances.FirstOrDefault(x => x.Id == instanceId);

        if (instanceToRemove == null) return;
        
        currentInstances.Remove(instanceToRemove);
        SaveInstances(currentInstances);
    }
}
