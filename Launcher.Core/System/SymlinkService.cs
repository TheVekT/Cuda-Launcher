using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Launcher.Core.Config.Abstractions;
using Launcher.Core.System.Abstractions;
using Launcher.Core.System.Models;

namespace Launcher.Core.System;

public class SymlinkService : ISymlinkService
{
    private readonly string _toolPath;
    private readonly ILauncherPathsService _pathsService;
    
    public SymlinkService(ILauncherPathsService pathsService)
    {
        _pathsService = pathsService;
        _toolPath = Path.Combine(_pathsService.BaseDirectory, "Launcher Helper.exe");
    }

    public void CreateSymlinksElevated(string sourceBase, string destBase, HashSet<string> inclusions)
    {
        if (!File.Exists(_toolPath))
        {
             throw new FileNotFoundException($"UAC Helper tool not found at {_toolPath}. Make sure Launcher Helper is built.");
        }
        
        var job = new SymlinkJob 
        { 
            SourcePath = sourceBase, 
            DestPath = destBase, 
            Inclusions = inclusions 
        };
        string tempJobFile = Path.Combine(Path.GetTempPath(), $"job_{Guid.NewGuid()}.json");
        string json = JsonSerializer.Serialize(job);
        File.WriteAllText(tempJobFile, json);
        
        var startInfo = new ProcessStartInfo
        {
            FileName = _toolPath,
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
        catch (Win32Exception)
        {
            throw new Exception("Administrator rights denied by user.");
        }
        finally
        {
            if (File.Exists(tempJobFile)) 
            {
                try { File.Delete(tempJobFile); } catch { }
            }
        }
    }
}