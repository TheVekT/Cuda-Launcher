using System.IO;
using Launcher.Updater.Models;

namespace Launcher.Updater.Helpers;

public static class UpdaterArgumentsParser
{
    public static UpdaterArgs ParseCommandLineArgs(string[] args)
    {
        int? pid = null;
        string targetDir = string.Empty;
        string targetVersion = string.Empty;
        string restartPath = string.Empty;
        string downloadUrl = string.Empty;
        string expectedSha256 = string.Empty;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (i + 1 < args.Length)
            {
                switch (arg.ToLowerInvariant())
                {
                    case "--pid":
                        if (int.TryParse(args[++i], out int parsedPid))
                            pid = parsedPid;
                        break;
                    case "--target":
                        targetDir = args[++i].Trim('\"');
                        break;
                    case "--target-version":
                        targetVersion = args[++i].Trim('\"');
                        break;
                    case "--restart":
                        restartPath = args[++i].Trim('\"');
                        break;
                    case "--url":
                    case "--download-url":
                        downloadUrl = args[++i].Trim('\"');
                        break;
                    case "--sha256":
                    case "--hash":
                        expectedSha256 = args[++i].Trim('\"');
                        break;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(targetDir))
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (Path.GetFileName(baseDir).Equals("temp", StringComparison.OrdinalIgnoreCase))
            {
                targetDir = Directory.GetParent(baseDir)?.FullName ?? baseDir;
            }
            else
            {
                targetDir = baseDir;
            }
        }

        return new UpdaterArgs(
            pid, 
            targetDir, 
            targetVersion, 
            restartPath, 
            downloadUrl, 
            expectedSha256);
    }
}