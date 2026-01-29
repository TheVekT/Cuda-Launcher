using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Добавлено для LINQ методов (FirstOrDefault)
using System.Text.Json;
using Launcher.Core.Models;

namespace Launcher.Core.Services.IO
{
    public interface IInstanceService
    {
        void SaveInstances(IEnumerable<MinecraftInstance> instances);
        List<MinecraftInstance> LoadInstances();
        
        // Добавлен метод удаления
        void DeleteInstance(string instanceId);
    }

    public class InstanceService : IInstanceService
    {
        private readonly string _storagePath;
        private readonly string _filePath;

        public InstanceService()
        {
            _storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _filePath = Path.Combine(_storagePath, "instances.json");
        }

        public void SaveInstances(IEnumerable<MinecraftInstance> instances)
        {
            if (!Directory.Exists(_storagePath))
            {
                Directory.CreateDirectory(_storagePath);
            }

            try
            {
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                };
                
                var json = JsonSerializer.Serialize(instances, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
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
                System.Diagnostics.Debug.WriteLine($"Ошибка загрузки инстансов: {ex.Message}");
                return new List<MinecraftInstance>();
            }
        }

        // Реализация удаления
        public void DeleteInstance(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return;

            // 1. Загружаем текущий список
            var currentInstances = LoadInstances();

            // 2. Ищем инстанс для удаления
            var instanceToRemove = currentInstances.FirstOrDefault(x => x.Id == instanceId);

            if (instanceToRemove != null)
            {
                // 3. Удаляем из списка
                currentInstances.Remove(instanceToRemove);

                // 4. Сохраняем обновленный список обратно в файл
                SaveInstances(currentInstances);
            }
        }
    }
}