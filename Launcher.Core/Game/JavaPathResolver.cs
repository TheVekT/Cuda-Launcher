using System.Diagnostics;
using System.Text.Json;
using CmlLib.Core;
using Launcher.Core.Game.Abstractions;
using Launcher.Core.Instances.Models;

namespace Launcher.Core.Game;

public class JavaPathResolver : IJavaPathResolver
{
    public string? ResolveJavaPath(MinecraftPath globalMcPath, MinecraftInstance instance) =>
        ResolveJavaPath(globalMcPath, instance.GameVersion);

    public string? ResolveJavaPath(MinecraftPath globalMcPath, string gameVersion)
    {
        string runtimeDir = globalMcPath.Runtime;
        if (!Directory.Exists(runtimeDir))
            return null;

        string osArch = OperatingSystem.IsWindows() ? "windows-x64" : (OperatingSystem.IsMacOS() ? "mac-os" : "linux");
        string exe = OperatingSystem.IsWindows() ? "javaw.exe" : "java";

        string? FindJava(string component)
        {
            var p1 = Path.Combine(runtimeDir, osArch, component, "bin", exe);
            if (File.Exists(p1)) return p1;
            var p2 = Path.Combine(runtimeDir, component, "bin", exe);
            return File.Exists(p2) ? p2 : null;
        }

        // 1. Check base version JSON manifest (e.g. versions/1.21.1/1.21.1.json)
        string baseJson = Path.Combine(globalMcPath.Versions, gameVersion, $"{gameVersion}.json");
        if (File.Exists(baseJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(baseJson));
                if (doc.RootElement.TryGetProperty("javaVersion", out var jv) &&
                    jv.TryGetProperty("component", out var comp) &&
                    comp.GetString() is { Length: > 0 } componentName)
                {
                    var found = FindJava(componentName);
                    if (found != null) return found;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Launch Warn] Failed to parse javaVersion from {baseJson}: {ex.Message}");
            }
        }

        // 2. Version heuristic fallback (works completely offline)
        string target = gameVersion switch
        {
            var v when v.StartsWith("26", StringComparison.OrdinalIgnoreCase) => "java-runtime-epsilon",
            var v when IsVersionAtLeast(v, 1, 20, 5) => "java-runtime-delta",
            var v when IsVersionAtLeast(v, 1, 18) => "java-runtime-gamma",
            var v when IsVersionAtLeast(v, 1, 17) => "java-runtime-alpha",
            _ => "jre-legacy"
        };

        return FindJava(target);
    }

    private static bool IsVersionAtLeast(string versionStr, int targetMajor, int targetMinor, int targetBuild = 0)
    {
        var parts = versionStr.Split('.');
        if (parts.Length >= 2 && int.TryParse(parts[0], out int major) && int.TryParse(parts[1], out int minor))
        {
            int build = parts.Length >= 3 && int.TryParse(parts[2], out int b) ? b : 0;
            if (major != targetMajor) return major > targetMajor;
            if (minor != targetMinor) return minor > targetMinor;
            return build >= targetBuild;
        }
        return false;
    }
}
