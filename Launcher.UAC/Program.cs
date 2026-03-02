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
        // 1. Меняем контракт на Inclusions (Белый список)
        public HashSet<string> Inclusions { get; set; }
    }

    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0) return 1;

            string jobFilePath = args[0];
            if (!File.Exists(jobFilePath)) return 1;

            try
            {
                var json = File.ReadAllText(jobFilePath);
                
                // Настраиваем парсер, чтобы он не придирался к регистру букв в JSON
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var job = JsonSerializer.Deserialize<SymlinkJob>(json, options);

                if (job == null || job.Inclusions == null) return 1;

                // ВАЖНО: При десериализации HashSet теряет настройку "игнорировать регистр".
                // Пересоздаем его с StringComparer.OrdinalIgnoreCase, чтобы "options.txt" и "Options.txt" считались одинаково.
                var safeInclusions = new HashSet<string>(job.Inclusions, StringComparer.OrdinalIgnoreCase);
                job.Inclusions = safeInclusions;

                ExecuteMkLink(job);
                
                return 0;
            }
            catch (Exception ex)
            {
                File.WriteAllText("symlink_error.log", ex.ToString());
                return 1;
            }
            finally
            {
                if (File.Exists(jobFilePath)) 
                {
                    try { File.Delete(jobFilePath); } catch { }
                }
            }
        }

        static void ExecuteMkLink(SymlinkJob job)
        {
            if (!Directory.Exists(job.SourcePath)) return;

            // 1. Ссылки на папки (/D)
            foreach (var dirPath in Directory.GetDirectories(job.SourcePath))
            {
                var dirName = new DirectoryInfo(dirPath).Name;
                
                // ЛОГИКА ВАЙТЛИСТА: Если папки НЕТ в нашем белом списке — просто пропускаем её
                if (!job.Inclusions.Contains(dirName)) continue;

                var destDir = Path.Combine(job.DestPath, dirName);
                
                if (Directory.Exists(destDir)) Directory.Delete(destDir);

                RunCmd($"/c mklink /D \"{destDir}\" \"{dirPath}\"");
            }

            // 2. Ссылки на файлы
            foreach (var filePath in Directory.GetFiles(job.SourcePath))
            {
                var fileName = Path.GetFileName(filePath);
                
                // ЛОГИКА ВАЙТЛИСТА: Если файла НЕТ в списке — пропускаем
                if (!job.Inclusions.Contains(fileName)) continue;

                var destFile = Path.Combine(job.DestPath, fileName);
                
                if (File.Exists(destFile)) File.Delete(destFile);

                RunCmd($"/c mklink \"{destFile}\" \"{filePath}\"");
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