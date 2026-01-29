using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Launcher.Core.Models;

namespace Launcher.Core.Services.IO
{
    public interface IInstanceService
    {
        void SaveInstances(IEnumerable<MinecraftInstance> instances);
        List<MinecraftInstance> LoadInstances();
    }

    public class InstanceService : IInstanceService
    {
        private readonly string _storagePath;
        private readonly string _filePath;

        public InstanceService()
        {
            // Portable путь: папка с .exe + Data
            _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _filePath = Path.Combine(_storagePath, "instances.json");
        }

        public void SaveInstances(IEnumerable<MinecraftInstance> instances)
        {
            // Создаем папку Data, если её нет
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }

            try
            {
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true // Красивый JSON, чтобы можно было читать глазами
                };
                
                var json = JsonSerializer.Serialize(instances, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                // Тут можно добавить логирование
                System.Diagnostics.Debug.WriteLine($"Ошибка сохранения инстансов: {ex.Message}");
            }
        }

        public List<MinecraftInstance> LoadInstances()
        {
            if (!File.Exists(_filePath))
            {
                return new List<MinecraftInstance>();
            }

            try
            {
                var json = File.ReadAllText(_filePath);
                var instances = JsonSerializer.Deserialize<List<MinecraftInstance>>(json);
                return instances ?? new List<MinecraftInstance>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки инстансов (файл поврежден?): {ex.Message}");
                return new List<MinecraftInstance>();
            }
        }
    }
}