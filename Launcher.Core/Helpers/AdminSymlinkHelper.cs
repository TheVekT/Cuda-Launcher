using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Launcher.Core.Services.System;

namespace Launcher.Core.Helpers;

public class SymlinkJob
{
    public string SourcePath { get; set; }
    public string DestPath { get; set; }
    public HashSet<string> Inclusions { get; set; } // Теперь это белый список
}

public static class AdminSymlinkHelper
{
    private static readonly string ToolPath = Path.Combine(LauncherPathsService.BaseDirectory, "Launcher Helper.exe");

    public static void CreateSymlinksElevated(string sourceBase, string destBase, HashSet<string> inclusions)
    {
        // Если не нашли рядом - возможно мы в Debug режиме и он лежит в папке сборки
        if (!File.Exists(ToolPath))
        {
             throw new FileNotFoundException($"UAC Helper tool not found at {ToolPath}. Make sure Launcher Helper is built.");
        }

        // 2. Создаем объект задачи (передаем Inclusions)
        var job = new SymlinkJob 
        { 
            SourcePath = sourceBase, 
            DestPath = destBase, 
            Inclusions = inclusions 
        };
        
        // Сохраняем во временный JSON
        string tempJobFile = Path.Combine(Path.GetTempPath(), $"job_{Guid.NewGuid()}.json");
        string json = JsonSerializer.Serialize(job);
        File.WriteAllText(tempJobFile, json);

        // 3. Запускаем спутник с правами Админа
        var startInfo = new ProcessStartInfo
        {
            FileName = ToolPath,
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
