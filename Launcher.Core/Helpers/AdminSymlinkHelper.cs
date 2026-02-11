using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Launcher.Core.Helpers
{
    public class SymlinkJob
    {
        public string SourcePath { get; set; }
        public string DestPath { get; set; }
        public HashSet<string> Exclusions { get; set; }
    }
    public static class AdminSymlinkHelper
    {
        public static void CreateSymlinksElevated(string sourceBase, string destBase, HashSet<string> exclusions)
        {
            // 1. Ищем наш exe-спутник
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // ВАЖНО: Имя файла должно совпадать с именем твоего нового проекта UAC!
            // Если проект называется Launcher.UAC, то и exe будет Launcher.UAC.exe
            string toolName = "Launcher Helper.exe"; 
            string toolPath = Path.Combine(baseDir, toolName);

            // Если не нашли рядом - возможно мы в Debug режиме и он лежит в папке сборки
            if (!File.Exists(toolPath))
            {

                 throw new FileNotFoundException($"UAC Helper tool not found at {toolPath}. Make sure Launcher.UAC is built.");
            }

            // 2. Создаем объект задачи (теперь класс SymlinkJob доступен)
            var job = new SymlinkJob 
            { 
                SourcePath = sourceBase, 
                DestPath = destBase, 
                Exclusions = exclusions 
            };
            
            // Сохраняем во временный JSON
            string tempJobFile = Path.Combine(Path.GetTempPath(), $"job_{Guid.NewGuid()}.json");
            string json = JsonSerializer.Serialize(job);
            File.WriteAllText(tempJobFile, json);

            // 3. Запускаем спутник с правами Админа
            var startInfo = new ProcessStartInfo
            {
                FileName = toolPath,
                Arguments = $"\"{tempJobFile}\"",
                UseShellExecute = true,
                Verb = "runas", 
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };

            try
            {
                var process = Process.Start(startInfo);
                process.WaitForExit(); 

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Symlink helper exited with code {process.ExitCode}. Check log file near exe.");
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Это исключение вылетает, если пользователь нажал "Нет" в окне UAC
                throw new Exception("Administrator rights denied by user.");
            }
            finally
            {
                // Удаляем временный файл задачи
                if (File.Exists(tempJobFile)) 
                {
                    try { File.Delete(tempJobFile); } catch { }
                }
            }
        }
    }
}