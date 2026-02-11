using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace Launcher.UAC
{
    public class SymlinkJob
    {
        public string SourcePath { get; set; }
        public string DestPath { get; set; }
        public HashSet<string> Exclusions { get; set; }
    }

    class Program
    {
        // Возвращаем int (код ошибки): 0 - успех, 1 - ошибка
        static int Main(string[] args)
        {
            // Ожидаем путь к файлу задачи как первый аргумент
            if (args.Length == 0) return 1;

            string jobFilePath = args[0];
            if (!File.Exists(jobFilePath)) return 1;

            try
            {
                var json = File.ReadAllText(jobFilePath);
                var job = JsonSerializer.Deserialize<SymlinkJob>(json);

                if (job == null) return 1;

                ExecuteMkLink(job);
                
                return 0; // Всё супер
            }
            catch (Exception ex)
            {
                // Пишем лог ошибки, так как консоли нет
                File.WriteAllText("symlink_error.log", ex.ToString());
                return 1; // Ошибка
            }
            finally
            {
                // Удаляем файл задачи
                if (File.Exists(jobFilePath)) File.Delete(jobFilePath);
            }
        }

        static void ExecuteMkLink(SymlinkJob job)
        {
            // 1. Ссылки на папки (/D)
            if (Directory.Exists(job.SourcePath))
            {
                foreach (var dirPath in Directory.GetDirectories(job.SourcePath))
                {
                    var dirName = new DirectoryInfo(dirPath).Name;
                    if (job.Exclusions != null && job.Exclusions.Contains(dirName)) continue;

                    var destDir = Path.Combine(job.DestPath, dirName);
                    
                    // Удаляем старую ссылку/папку, если есть, чтобы обновить
                    // (Осторожно: Directory.Delete удалит реальные файлы, если это не ссылка, 
                    // но PreparePartial в основном лаунчере гарантирует, что мы работаем с инстансом)
                    if (Directory.Exists(destDir)) Directory.Delete(destDir);

                    RunCmd($"/c mklink /D \"{destDir}\" \"{dirPath}\"");
                }

                // 2. Ссылки на файлы
                foreach (var filePath in Directory.GetFiles(job.SourcePath))
                {
                    var fileName = Path.GetFileName(filePath);
                    if (job.Exclusions != null && job.Exclusions.Contains(fileName)) continue;

                    var destFile = Path.Combine(job.DestPath, fileName);
                    
                    if (File.Exists(destFile)) File.Delete(destFile);

                    RunCmd($"/c mklink \"{destFile}\" \"{filePath}\"");
                }
            }
        }

        static void RunCmd(string arguments)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            
            using var p = Process.Start(psi);
            p?.WaitForExit();
        }
    }
}