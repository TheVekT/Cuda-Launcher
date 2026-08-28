using System.Diagnostics;
using System.Text.Json;

namespace Launcher.UAC;

public class SymlinkJob(string sourcePath, string destPath, HashSet<string> inclusions)
{
    public string SourcePath { get; init; } = sourcePath;
    public string DestPath { get; init; } = destPath;
    public HashSet<string> Inclusions { get; set; } = inclusions;
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
            
            // Configure the parser to be case-insensitive when reading JSON properties
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var job = JsonSerializer.Deserialize<SymlinkJob>(json, options);

            if (job == null) return 1;
            
            // English: When deserializing, HashSet loses the "ignore case" setting.
            // We recreate it with StringComparer.OrdinalIgnoreCase so that "options.txt" and "Options.txt" are considered the same.
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
                try
                {
                    File.Delete(jobFilePath);
                }
                catch
                {
                    Debug.WriteLine("Failed to delete symlink job file.");
                }
            }
        }
    }

    static void ExecuteMkLink(SymlinkJob job)
    {
        if (!Directory.Exists(job.SourcePath)) return;

        // 1. Make directory links for folders
        foreach (var dirPath in Directory.GetDirectories(job.SourcePath))
        {
            var dirName = new DirectoryInfo(dirPath).Name;
            
            if (!job.Inclusions.Contains(dirName)) continue;

            var destDir = Path.Combine(job.DestPath, dirName);
            
            if (Directory.Exists(destDir)) Directory.Delete(destDir);

            RunCmd($"/c mklink /D \"{destDir}\" \"{dirPath}\"");
        }

        // 2. Make file links
        foreach (var filePath in Directory.GetFiles(job.SourcePath))
        {
            var fileName = Path.GetFileName(filePath);
            
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
