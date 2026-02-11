using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Launcher.Core.Helpers
{
    public static class AdminSymlinkHelper
    {
        public static void CreateSymlinksElevated(string sourceBase, string destBase, HashSet<string> exclusions)
        {
            var commands = new StringBuilder();
            
            // Настройка кодировки для кириллицы в путях
            commands.AppendLine("@echo off");
            commands.AppendLine("chcp 65001 > nul"); 

            // 1. Проходим по ПАПКАМ (используем Symlink /D, чтобы работать между дисками)
            // Junction тоже работает, но Symlink универсальнее для "прозрачности"
            foreach (var dirPath in Directory.GetDirectories(sourceBase))
            {
                var dirName = new DirectoryInfo(dirPath).Name;
                if (exclusions.Contains(dirName)) continue;

                var destDir = Path.Combine(destBase, dirName);
                
                // Команда: mklink /D "куда" "откуда"
                // Важно: оборачиваем пути в кавычки на случай пробелов
                commands.AppendLine($"mklink /D \"{destDir}\" \"{dirPath}\"");
            }

            // 2. Проходим по ФАЙЛАМ (Symlink для файлов)
            foreach (var filePath in Directory.GetFiles(sourceBase))
            {
                var fileName = Path.GetFileName(filePath);
                if (exclusions.Contains(fileName)) continue;

                var destFile = Path.Combine(destBase, fileName);

                // Команда: mklink "куда" "откуда"
                commands.AppendLine($"mklink \"{destFile}\" \"{filePath}\"");
            }

            // Если команд нет - выходим
            if (commands.Length < 50) return;

            // 3. Создаем временный bat-файл
            string tempBatPath = Path.Combine(Path.GetTempPath(), $"minecraft_links_{Guid.NewGuid()}.bat");
            File.WriteAllText(tempBatPath, commands.ToString(), Encoding.UTF8);

            try
            {
                // 4. Запускаем CMD от имени Администратора
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{tempBatPath}\"", // /c выполнит и закроет
                    UseShellExecute = true,              // Обязательно true для Verb
                    Verb = "runas",                      // <--- МАГИЯ: Запрос прав Админа
                    WindowStyle = ProcessWindowStyle.Hidden, // Скрываем окно
                    CreateNoWindow = true
                };

                var process = Process.Start(startInfo);
                process.WaitForExit(); // Ждем завершения
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // Пользователь нажал "Нет" в UAC
                throw new Exception("User cancelled UAC prompt. Symlinks could not be created.");
            }
            finally
            {
                // Удаляем мусор за собой
                if (File.Exists(tempBatPath)) File.Delete(tempBatPath);
            }
        }
    }
}