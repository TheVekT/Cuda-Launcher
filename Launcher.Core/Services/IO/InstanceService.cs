using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Launcher.Core.Models;

namespace Launcher.Core.Services.IO
{
    public interface IInstanceService
    {
        void SaveInstances(IEnumerable<MinecraftInstance> instances);
        List<MinecraftInstance> LoadInstances();
        void DeleteInstance(string instanceId);
    }

    public class InstanceService : IInstanceService
    {
        private readonly string _instancesFolderPath; 
        private readonly string _jsonFilePath;        

        public InstanceService()
        {
            // Формируем путь: .../Data/Instances
            _instancesFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Instances");
            _jsonFilePath = Path.Combine(_instancesFolderPath, "instances.json");
        }

        public void SaveInstances(IEnumerable<MinecraftInstance> instances)
        {
            // Убеждаемся, что папка Data/Instances существует
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
                Debug.WriteLine($"Ошибка сохранения: {ex.Message}");
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

            if (instanceToRemove != null)
            {
                currentInstances.Remove(instanceToRemove);
                SaveInstances(currentInstances);
                
                // В будущем здесь добавим вызов:
                // _fileSystemService.DeleteInstanceFolder(instanceToRemove.Name);
            }
        }
    }
}